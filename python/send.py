from flask import Flask, request, jsonify

app = Flask(__name__)

### 人臉辨識API ###
from face_recognize.image import FaceTracker

# 請設定face database檔案絕對路徑
face_tracker = FaceTracker(storage_path='絕對路徑/face_database.xlsx')

@app.route('/face-recognition', methods=['POST'])
def face_recognition_api():
    try:
        print("=== 收到人臉辨識請求 ===")
        data = request.get_json()
        images = data.get("images", None)
        if not images or not isinstance(images, list):
            return jsonify({"error": "請提供 images 陣列"}), 400

        id_count = {}
        errors = []
        for base64_img in images:
            result = face_tracker.process_base64_image(base64_img)
            if result["success"]:
                fid = result["id"]
                id_count[fid] = id_count.get(fid, 0) + 1
                print(f"識別到人臉 ID: {fid}", flush=True)
            else:
                errors.append(result["error"])

        if not id_count:
            return jsonify({"success": False, "errors": errors, "id": None})

        final_id = max(id_count, key=id_count.get)
        return jsonify({"success": True, "id": final_id, "errors": errors})

    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500

##################

### 推薦餐點API ###
from api_integration.recommender import recommend_from_payload
@app.post("/recommend")
def api_recommend():
    print("=== 收到推薦餐點請求 ===")
    try:
        payload = request.get_json(force=True)  # 取得 JSON body
        user    = request.args.get("user", "guest") # 預設 user 為 guest
        topk    = int(request.args.get("topk", 5)) # 預設 topk 為 5
        window  = int(request.args.get("window", 14)) # 預設 window 為 14
        when    = request.args.get("when", None) # 預設為 None (即現在時間)

        result = recommend_from_payload(payload, user=user, when=when, topk=topk, window=window)
        return jsonify(result)
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500
    
##################

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)
