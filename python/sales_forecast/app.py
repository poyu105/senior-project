from flask import Flask, request, jsonify, Response # <--- 匯入 Response
import difference_model  # 匯入模型工具函式
import os
import json
from collections import OrderedDict

app = Flask(__name__)

# --- Flask 設定 ---
app.config['JSON_SORT_KEYS'] = False


# --- API 路由 (Endpoints) ---

@app.route("/process-data", methods=["POST"])
def process_data():
    """接收歷史銷售資料和預測目標，回傳預測結果"""
    if not request.is_json:
        return jsonify({"error": "請求的 Content-Type 必須是 application/json"}), 400

    data = request.get_json()
    sales_data = data.get("sales_data")
    prediction_data = data.get("prediction_data")

    if not sales_data or not prediction_data:
        return jsonify({"error": "請求中缺少 'sales_data' 或 'prediction_data'"}), 400

    result, error, status_code = difference_model.predict_sales(sales_data, prediction_data)

    if error:
        return jsonify({"error": f"內部伺服器錯誤: {error}"}), status_code

    json_string = json.dumps(result, ensure_ascii=False)
    
    return Response(json_string, content_type='application/json; charset=utf-8', status=status_code)


@app.route("/feedback", methods=["POST"])
def feedback():
    """接收包含多個品項的實際銷售回饋，更新模型"""
    if not request.is_json:
        return jsonify({"error": "請求的 Content-Type 必須是 application/json"}), 400

    data = request.get_json()
    
    message, error, status_code = difference_model.handle_feedback(data)

    if error:
        return jsonify({"error": f"內部伺服器錯誤: {error}"}), status_code

    return jsonify({"message": message}), status_code


# --- 主程式 ---
if __name__ == "__main__":
    if not os.path.exists(difference_model.SALES_FILE):
        print(f"錯誤：找不到主要的訓練資料檔案 '{difference_model.SALES_FILE}'，伺服器無法啟動。")
        print("請確認此檔案與 app.py 放在同一個資料夾中。")
    else:
        print("啟動 Flask 伺服器中，請在本機 8000 port 進行 API 測試。")
        app.run(host="0.0.0.0", port=8000, debug=False)

