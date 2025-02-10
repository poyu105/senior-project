import numpy as np
import cv2
import os
import pandas as pd
import json
from scipy.spatial.distance import cosine
from PIL import Image
import torch
import uuid
from facenet_pytorch import InceptionResnetV1
import torchvision.transforms as transforms
import mediapipe as mp

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
    def __init__(self,
                 similarity_threshold=0.8,
                 max_features=20,
                 distance_threshold=0.3,
                 storage_path='face_database.xlsx'):
        self.similarity_threshold = similarity_threshold
        self.max_features = max_features
        self.distance_threshold = distance_threshold
        self.storage_path = storage_path
        self.known_faces = self.load_face_database()

    def load_face_database(self):
        """從 Excel 載入人臉數據庫"""
        if not os.path.exists(self.storage_path):
            print("Excel 人臉數據庫不存在，將初始化新數據庫。")
            return {}

        try:
            df = pd.read_excel(self.storage_path)
            known_faces = {}
            for _, row in df.iterrows():
                person_id = row['ID']
                features = np.array(json.loads(row['Features']))  # 轉回 NumPy 陣列
                known_faces[person_id] = {
                    'features': [features],
                    'last_seen': row['Last Seen']
                }
            return known_faces
        except Exception as e:
            print(f"載入 Excel 時發生錯誤: {e}，將初始化新數據庫。")
            return {}

    def save_face_database(self):
        """保存人臉數據庫到 Excel"""
        data = []
        for person_id, info in self.known_faces.items():
            feature_str = json.dumps(info['features'][0].tolist())  # 轉換為 JSON 格式
            data.append([person_id, feature_str, info['last_seen']])

        df = pd.DataFrame(data, columns=['ID', 'Features', 'Last Seen'])
        df.to_excel(self.storage_path, index=False)

    def extract_features(self, face_img):
        """提取人臉特徵"""
        if face_img is None or face_img.size == 0:
            raise ValueError("Invalid face image")
        face_tensor = transform(Image.fromarray(cv2.cvtColor(face_img, cv2.COLOR_BGR2RGB))).unsqueeze(0).to(device)
        features_list = []
        with torch.no_grad():
            for _ in range(3):  # 減少採樣次數
                features = model(face_tensor).cpu().numpy().flatten()
                features_list.append(features)
        mean_feature = np.mean(features_list, axis=0)
        return mean_feature

    def find_or_create_id(self, features):
        """尋找或創建人臉 ID"""
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


def main():
    face_tracker = FaceTracker()
    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        print("無法開啟攝影機！")
        return

    try:
        while True:
            ret, frame = cap.read()
            if not ret:
                print("無法讀取影像")
                break
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            results = mp_face_detection.process(rgb_frame)
            if results.detections:
                for detection in results.detections:
                    bboxC = detection.location_data.relative_bounding_box
                    ih, iw, _ = frame.shape
                    x, y, w, h = int(bboxC.xmin * iw), int(bboxC.ymin * ih), int(bboxC.width * iw), int(
                        bboxC.height * ih)
                    face = frame[y:y + h, x:x + w]
                    if face.shape[0] > 0 and face.shape[1] > 0:
                        try:
                            aligned_face = cv2.resize(face, (160, 160))
                            features = face_tracker.extract_features(aligned_face)
                            person_id = face_tracker.find_or_create_id(features)
                            cv2.rectangle(frame, (x, y), (x + w, y + h), (255, 0, 0), 2)
                            cv2.putText(frame, f"ID: {person_id}", (x, y - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5,
                                        (255, 0, 0), 2)
                        except Exception as e:
                            print(f"處理人臉時出錯: {e}")
            cv2.imshow('Face Detection', frame)
            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
    finally:
        face_tracker.save_face_database()
        cap.release()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
