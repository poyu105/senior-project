# app.py

from flask import Flask, request, jsonify
# 核心邏輯的檔案名稱已從 recommendation_logic 改為 recommender
from recommender import generate_recommendations

# 建立 Flask 應用
app = Flask(__name__)

# 設定 API 路由 (route)
@app.route('/recommend', methods=['POST'])
def recommend_meals():
    """
    接收 JSON 資料，呼叫推薦邏輯，並回傳推薦結果。
    """
    
    # 1. 獲取傳入的 JSON 資料
    data = request.get_json()

    # 2. 驗證資料
    if not data or not isinstance(data, list) or len(data) == 0:
        return jsonify({"error": "無效的 JSON 格式。預期應為 [ { ... } ]"}), 400

    try:
        # 取得 List 中的第一個元素
        input_data = data[0] 

        # 3. 呼叫 "主程式" (核心邏輯)
        #    現在是從 recommender.py 導入的
        recommendations_list = generate_recommendations(input_data)

        # 4. 將結果列表直接回傳
        return jsonify(recommendations_list)

    except Exception as e:
        return jsonify({"error": f"處理請求時發生錯誤: {str(e)}"}), 500

# 啟動伺服器
if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)