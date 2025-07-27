import os
import json
import uuid
import numpy as np
import pandas as pd
from scipy.spatial.distance import cosine
from PIL import Image
import cv2
import torch
from facenet_pytorch import InceptionResnetV1
import torchvision.transforms as transforms
import mediapipe as mp

class FaceTracker:
    def __init__(self, storage_path):
        self.storage_path = storage_path
        self.similarity_threshold = 0.8
        self.max_features = 20
        self.distance_threshold = 0.3

        # 初始化模型
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        self.model = InceptionResnetV1(pretrained='vggface2').eval().to(self.device)
        self.transform = transforms.Compose([
            transforms.Resize((160, 160)),
            transforms.ToTensor(),
            transforms.Normalize(mean=[0.5]*3, std=[0.5]*3)
        ])
        self.mp_face_detection = mp.solutions.face_detection.FaceDetection(model_selection=1, min_detection_confidence=0.5)

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
                known_faces[person_id] = {'features': [features], 'last_seen': row['Last Seen']}
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
        face_tensor = self.transform(Image.fromarray(cv2.cvtColor(face_img, cv2.COLOR_BGR2RGB))).unsqueeze(0).to(self.device)
        features_list = []
        with torch.no_grad():
            for _ in range(3):
                features = self.model(face_tensor).cpu().numpy().flatten()
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

    def process_base64_image(self, base64_image_str):
        import base64
        from io import BytesIO
        try:
            if base64_image_str.startswith("data:image"):
                base64_image_str = base64_image_str.split(",", 1)[1]

            image_data = base64.b64decode(base64_image_str)
            image = Image.open(BytesIO(image_data)).convert("RGB")
            image_np = np.array(image).copy()

            results = self.mp_face_detection.process(image_np)
            if not results.detections:
                return {"success": False, "id": None, "error": "No face detected"}

            ih, iw, _ = image_np.shape
            bboxC = results.detections[0].location_data.relative_bounding_box
            x, y, w, h = int(bboxC.xmin * iw), int(bboxC.ymin * ih), int(bboxC.width * iw), int(bboxC.height * ih)
            face = image_np[y:y + h, x:x + w]

            if face.shape[0] == 0 or face.shape[1] == 0:
                return {"success": False, "id": None, "error": "Face area is empty"}

            aligned_face = cv2.resize(face, (160, 160))
            features = self.extract_features(aligned_face)
            face_id = self.find_or_create_id(features)

            return {"success": True, "id": face_id, "error": None}

        except Exception as e:
            return {"success": False, "id": None, "error": str(e)}
