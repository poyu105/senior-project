import requests
import base64
from scipy.spatial.distance import cosine

# 讀取本地圖片並轉成 base64
with open("0421.jpg", "rb") as img_file:
    base64_str = "data:image/png;base64," + base64.b64encode(img_file.read()).decode()

# 發送請求到你的 Flask API
response = requests.post("http://127.0.0.1:5000/face-recognition", json={"image": base64_str})

# 印出結果
print("伺服器回應：", response.status_code)
print("內容：", response.json())
