from flask import Flask, request, jsonify
import sklearn
import pandas
import numpy
import importlib.metadata

flask_version = importlib.metadata.version("flask")

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
    finally:
        print("=== 人臉辨識請求處理完畢 ===")
##################

### 推薦餐點API ###
from recommend.recommender import generate_recommendations
@app.route("/recommend", methods=["POST"])
def api_recommend():
    print("=== 收到推薦餐點請求 ===")
    try:
        data = request.get_json()

        if not data or not isinstance(data, list) or len(data) == 0:
            return jsonify({"success": False, "error": "無效的 JSON 格式。"}), 400

        input_data = data[0]
        recommendations_list = generate_recommendations(input_data)

        return jsonify({"success": True, "meals": recommendations_list, "msg": "成功取得推薦結果"})
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500
    finally:
        print("=== 推薦餐點請求處理完畢 ===")
##################

### 銷售預測API ###
from sales_forecast import difference_model
@app.route("/sales-forecast", methods=["POST"])
def process_data():
    print("=== 收到銷售預測請求 ===")
    try:
        data = request.get_json()
        sales_data = data.get("sales_data")
        prediction_data = data.get("prediction_data")
        
        if not sales_data or not prediction_data:
            return jsonify({"success": False, "errors": "請求中缺少 'sales_data' 或 'prediction_data'"}), 400
        
        success, result, msg = difference_model.predict_sales(sales_data, prediction_data) # 呼叫模型預測函式
        
        return jsonify({"success": True, "result": result, "msg": msg})
    except Exception as e:
        print(f"send發生例外錯誤{str(e)}")
        return jsonify({"success": False, "error": str(e)}), 500
    finally:
        print("=== 銷售預測請求處理完畢 ===")
##################

### 模型更新API ###
@app.route("/feedback", methods=["POST"])
def feedback():
    print("=== 收到模型更新請求 ===")
    try:
        data = request.get_json()
        success, message = difference_model.handle_feedback(data) # 處理回饋並更新模型

        return jsonify({"success": success, "message": message})
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500
    finally:
        print("=== 模型更新請求處理完畢 ===")
##################

# --- 主程式 ---
if __name__ == "__main__":
    print("===== 啟動 Flask 伺服器中，請在本機 5000 port 進行 API 測試。 =====")
    print("===== 版本資訊 =====")
    print(f"flask 版本: {flask_version}")
    print(f"sklearn 版本: {sklearn.__version__}")
    print(f"pandas 版本: {pandas.__version__}")
    print(f"numpy 版本: {numpy.__version__}")
    print("====================")
    app.run(host="0.0.0.0", port=5000)
    print("===== Flask 伺服器已停止。 =====")
