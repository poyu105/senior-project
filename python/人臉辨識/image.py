import sys
import numpy as np
import cv2
import os
import pandas as pd
import json
import base64
from io import BytesIO
from PIL import Image
import torch
import uuid
from facenet_pytorch import InceptionResnetV1
import torchvision.transforms as transforms
import mediapipe as mp
from scipy.spatial.distance import cosine

# 初始化模型
device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
model = InceptionResnetV1(pretrained='vggface2').eval().to(device)

# 初始化 Mediapipe
mp_face_detection = mp.solutions.face_detection.FaceDetection(model_selection=1, min_detection_confidence=0.5)

# 圖像預處理
transform = transforms.Compose([
    transforms.Resize((160, 160)),
    transforms.ToTensor(),
    transforms.Normalize(mean=[0.5, 0.5, 0.5], std=[0.5, 0.5, 0.5]),
])

class FaceTracker:
    def __init__(self, similarity_threshold=0.8, max_features=20, distance_threshold=0.3, storage_path='face_database.xlsx'):
        self.similarity_threshold = similarity_threshold
        self.max_features = max_features
        self.distance_threshold = distance_threshold
        self.storage_path = storage_path
        self.known_faces = self.load_face_database()

    def load_face_database(self):
        if not os.path.exists(self.storage_path):
            print("Excel 人臉數據庫不存在，將初始化新數據庫。", flush=True)
            return {}
        try:
            df = pd.read_excel(self.storage_path)
            known_faces = {}
            for _, row in df.iterrows():
                person_id = row['ID']
                features = np.array(json.loads(row['Features']))
                known_faces[person_id] = {
                    'features': [features],
                    'last_seen': row['Last Seen']
                }
            return known_faces
        except Exception as e:
            print(f"載入 Excel 時發生錯誤: {e}，將初始化新數據庫。", flush=True)
            return {}

    def save_face_database(self):
        data = []
        for person_id, info in self.known_faces.items():
            feature_str = json.dumps(info['features'][0].tolist())
            data.append([person_id, feature_str, info['last_seen']])
        df = pd.DataFrame(data, columns=['ID', 'Features', 'Last Seen'])
        df.to_excel(self.storage_path, index=False)

    def extract_features(self, face_img):
        if face_img is None or face_img.size == 0:
            raise ValueError("Invalid face image")
        face_tensor = transform(Image.fromarray(cv2.cvtColor(face_img, cv2.COLOR_BGR2RGB))).unsqueeze(0).to(device)
        features_list = []
        with torch.no_grad():
            for _ in range(3):
                features = model(face_tensor).cpu().numpy().flatten()
                features_list.append(features)
        mean_feature = np.mean(features_list, axis=0)
        return mean_feature

    def find_or_create_id(self, features):
        if features is None or len(features) == 0:
            raise ValueError("Invalid features")
        best_match = None
        best_distance = float('inf')
        for person_id, info in self.known_faces.items():
            distances = [cosine(features, feat) for feat in info['features']]
            min_distance = min(distances)
            if min_distance < best_distance:
                best_distance = min_distance
                best_match = person_id
        if best_distance < self.distance_threshold:
            self.known_faces[best_match]['features'].append(features)
            if len(self.known_faces[best_match]['features']) > self.max_features:
                self.known_faces[best_match]['features'].pop(0)
            return best_match
        new_id = str(uuid.uuid4())[:8]
        self.known_faces[new_id] = {'features': [features], 'last_seen': 0}
        self.save_face_database()
        return new_id

face_tracker = FaceTracker()

def process_base64_image(base64_image_str):
    try:
        # 去掉 data:image/...;base64, 前綴
        if base64_image_str.startswith("data:image"):
            base64_image_str = base64_image_str.split(",", 1)[1]

        image_data = base64.b64decode(base64_image_str)
        image = Image.open(BytesIO(image_data)).convert("RGB")
        image_np = np.array(image).copy()

        # Mediapipe 偵測人臉
        results = mp_face_detection.process(image_np)
        if not results.detections:
            print("unknown", flush=True)
            return

        # 擷取第一個人臉區塊
        ih, iw, _ = image_np.shape
        bboxC = results.detections[0].location_data.relative_bounding_box
        x, y, w, h = int(bboxC.xmin * iw), int(bboxC.ymin * ih), int(bboxC.width * iw), int(bboxC.height * ih)
        face = image_np[y:y + h, x:x + w]

        if face.shape[0] == 0 or face.shape[1] == 0:
            print("unknown", flush=True)
            return

        aligned_face = cv2.resize(face, (160, 160))
        features = face_tracker.extract_features(aligned_face)
        face_id = face_tracker.find_or_create_id(features)

        print(face_id, flush=True)

    except Exception as e:
        print(f"error:{str(e)}", flush=True)

if __name__ == "__main__":
    base64_input = sys.stdin.readline().strip()
    process_base64_image(base64_input)
