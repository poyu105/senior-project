from flask import Flask, request, jsonify
from image import FaceTracker

app = Flask(__name__)

# 請設定face database檔案絕對路徑
face_tracker = FaceTracker(storage_path='絕對路徑/face_database.xlsx')

@app.route('/face-recognition', methods=['POST'])
def face_recognition_api():
    try:
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

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000)
