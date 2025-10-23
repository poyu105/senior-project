# api_server.py
from flask import Flask, request, jsonify
from recommender import recommend_from_payload

app = Flask(__name__)

@app.get("/ping")
def ping():
    return jsonify({"status": "ok"})

@app.post("/api/recommend")
def api_recommend():
    try:
        payload = request.get_json(force=True)  # 取得 JSON body
        user    = request.args.get("user", "guest")
        topk    = int(request.args.get("topk", 5))
        window  = int(request.args.get("window", 14))
        when    = request.args.get("when", None)

        result = recommend_from_payload(payload, user=user, when=when, topk=topk, window=window)
        return jsonify(result)
    except Exception as e:
        return jsonify({"error": str(e)}), 400

if __name__ == "__main__":
    app.run(debug=True)
