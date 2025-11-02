import pandas as pd
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import LabelEncoder
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score # <--- 新增匯入
import joblib
import os
import csv
import json 
from datetime import datetime
from collections import OrderedDict
import numpy as np

# --- 檔案設定 ---
BASE_FILE_PATH = os.path.dirname(os.path.abspath(__file__)) # 取得目前檔案所在目錄
SALES_FILE = os.path.join(BASE_FILE_PATH, "sales_data.csv")
FEEDBACK_FILE = os.path.join(BASE_FILE_PATH, "feedback_log.csv")
MODEL_FILE = os.path.join(BASE_FILE_PATH, "sales_model.pkl")
METRICS_FILE = os.path.join(BASE_FILE_PATH, "model_metrics.json") 

# --- 核心商業邏輯 ---

def get_season(date_obj):
    month = date_obj.month
    if month in [3, 4, 5]:
        return 'sp'
    elif month in [6, 7, 8]:
        return 'su'
    elif month in [9, 10, 11]:
        return 'au'
    else:
        return 'wi'

def _preprocess_data(df):
    df['date'] = pd.to_datetime(df['date'], errors='coerce')
    df.dropna(subset=['date'], inplace=True)
    df['is_weekend'] = df['date'].dt.dayofweek.isin([5, 6]).astype(int)
    df['season'] = df['date'].apply(get_season)
    return df

#訓練模型kernel
def train_model():
    """使用 SALES_FILE 的資料來訓練或重新訓練模型，並評估準確度"""
    print("正在檢查訓練資料檔案路徑：", os.path.abspath(SALES_FILE))
    if not os.path.exists(SALES_FILE):
        print(f"錯誤：找不到 {SALES_FILE}，無法訓練模型。")
        return False, None, "找不到 sales_data.csv，無法訓練模型。"
    try:
        df = pd.read_csv(SALES_FILE)
        df = _preprocess_data(df)
        encoders = {}
        for col in ["type", "weather", "season"]:
            le = LabelEncoder()
            df[col] = le.fit_transform(df[col].astype(str))
            encoders[col] = le
        X = df[["type", "weather", "season", "is_weekend"]]
        y = df["amount"]
        model = LinearRegression()
        model.fit(X, y)
        joblib.dump((model, encoders), MODEL_FILE)
        print("模型訓練完成並已儲存。")
        
        # --- 新增計算模型準確度的程式碼 ---
        try:
            y_pred = model.predict(X)
            mae = mean_absolute_error(y, y_pred)
            mse = mean_squared_error(y, y_pred)
            rmse = np.sqrt(mse)
            r2 = r2_score(y, y_pred)
            
            metrics = {
                "last_trained_on": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                "training_data_rows": len(df),
                "mae": round(mae, 4),
                "rmse": round(rmse, 4),
                "r2_score": round(r2, 4)
            }
            
            # 將指標儲存到 JSON 檔案
            with open(METRICS_FILE, 'w', encoding='utf-8') as f:
                json.dump(metrics, f, indent=4, ensure_ascii=False)
            
            # 印出指標
            print("\n--- 模型準確度---")
            print(f"| 平均絕對誤差 (MAE): {metrics['mae']:.4f}")
            print(f"| 均方根誤差 (RMSE): {metrics['rmse']:.4f}")
            print(f"| R-squared (R²): {metrics['r2_score']:.4f}")

        except Exception as e:
            print(f"計算模型準確度時發生錯誤: {e}")
        # --- 新增程式碼結束 ---

        return True, (model, encoders), None
    except Exception as e:
        error_msg = f"模型訓練失敗: {repr(e)}"
        print(error_msg)
        return False, None, error_msg

#銷售預測
def predict_sales(sales_data, prediction_data):
    """根據提供的歷史資料和預測目標，進行銷售預測"""
    print("正在檢查模型檔案路徑：", MODEL_FILE)
    if not os.path.exists(MODEL_FILE):
        print("模型檔案不存在，正在進行首次訓練...")
        success, result, msg = train_model()
        if not success:
            return False, result, msg
    
    try:
        model, encoders = joblib.load(MODEL_FILE)
        if hasattr(model, 'feature_names_in_') and 'name' in model.feature_names_in_:
            print("偵測到舊版模型 (包含 'name' 特徵)，將刪除並重新訓練...")
            os.remove(MODEL_FILE)
            success, result, msg = train_model()
            if not success:
                return False, result, f"刪除舊模型後重新訓練失敗: {msg}"
            model, encoders = joblib.load(MODEL_FILE)
    except Exception as e:
        print(f"載入模型失敗 ({e})，嘗試刪除並重新訓練...")
        if os.path.exists(MODEL_FILE):
            os.remove(MODEL_FILE)
        success, result, msg = train_model()
        if not success:
            return False, result, f"刪除損毀模型後重新訓練失敗: {msg}"
        model, encoders = joblib.load(MODEL_FILE)

    try:
        target_date_str = prediction_data.get('date')
        target_weather = prediction_data.get('weather')
        
        if not all([target_date_str, target_weather]):
            return False, None, "prediction_data 中缺少 'date' 或 'weather'"

        try:
            target_date = datetime.strptime(target_date_str, '%Y-%m-%d')
        except ValueError:
            return False, None, f"預測日期格式不正確: {target_date_str}，應為 YYYY-MM-DD"
        
        target_season = prediction_data.get('season')

        is_weekend_target = 1 if target_date.weekday() >= 5 else 0

        past_df = pd.DataFrame(sales_data)
        unique_meals = past_df[['meal_id', 'type', 'name', 'cost']].drop_duplicates()

        if unique_meals.empty:
            return False, None, "提供的 sales_data 為空或無效"

        predict_df = unique_meals.copy()
        predict_df['weather'] = target_weather
        predict_df['season'] = target_season
        predict_df['is_weekend'] = is_weekend_target
        
        X_pred = predict_df.copy()
        for col in ["type", "weather", "season"]:
            le = encoders.get(col)
            if le:
                X_pred[col] = X_pred[col].apply(lambda x: le.transform([x])[0] if x in le.classes_ else -1)
            else:
                return False, None, f"模型中缺少 '{col}' 的編碼器"
        
        X_pred_valid = X_pred[~X_pred.isin([-1]).any(axis=1)]
        prediction_list = []
        if not X_pred_valid.empty:
            predictions = model.predict(X_pred_valid[["type", "weather", "season", "is_weekend"]])
            predict_df.loc[X_pred_valid.index, 'predicted_amount'] = predictions
            prediction_list = [
                {
                    "meal_id": row['meal_id'],
                    "name": row['name'],
                    "sales": int(round(row.get('predicted_amount', 0))) * int(round(row.get('cost', 0))),
                    "cost": row["cost"],
                    "amount": int(round(row.get('predicted_amount', 0)))
                }
                for _, row in predict_df.iterrows()
            ]

        result = {
            "forecast_date": target_date_str,
            "season": target_season,
            "weather": target_weather,
            "prediction_list": prediction_list
        }
        
        return True, result, None

    except Exception as e:
        return False, None, f"預測過程中發生錯誤: {repr(e)}"

#使用回饋資料更新sec
def update_model_with_feedback(new_records_df):
    try:
        columns_to_save = ['date', 'season', 'weather', 'type', 'amount']
        df_to_save = new_records_df[columns_to_save]
        df_to_save.to_csv(
            SALES_FILE, 
            mode='a', 
            header=not os.path.exists(SALES_FILE), 
            index=False,
            quoting=csv.QUOTE_NONE,
            escapechar='\\'
        )
        print("銷售資料已更新，正在重新訓練模型...")
        success, error, _ = train_model()
        if not success:
            return False, f"重新訓練時發生錯誤: {error}"
        return True, "模型已根據最新回饋資料重新訓練完成。"
    except Exception as e:
        error_msg = f"寫入銷售資料或重新訓練時發生錯誤: {repr(e)}"
        print(error_msg)
        return False, error_msg

#處理訓練main
def handle_feedback(data):
    feedback_date_str = data.get("date")
    weather = data.get("weather")
    feedback_items = data.get("data")
    season = data.get("season")

    if not all([feedback_date_str, weather, feedback_items, season]):
        return False, "請求中缺少 'date', 'weather', 'data', 'season' 欄位"
    
    new_records = []
    log_records = []
    for item in feedback_items:
        real_amount = item.get("real_amount")
        if real_amount is None or not isinstance(real_amount, (int, float)):
            print(f"警告：在回饋項目 {item} 中找不到有效的 'real_amount'，將跳過此紀錄。")
            continue 

        new_records.append({
            "date": feedback_date_str,
            "season": season,
            "weather": weather,
            "type": item.get("type"),
            "amount": real_amount, 
            "meal_id": item.get("meal_id"),
            "name": item.get("name")
        })
        
        log_records.append(OrderedDict([
            ("date", feedback_date_str),
            ("meal_id", item.get("meal_id")),
            ("name", item.get("name")),
            ("type", item.get("type")),
            ("predicted_amount", item.get("predicted_amount")),
            ("real_amount", real_amount),
            ("weather", weather),
            ("season", season)
        ]))

    if not new_records:
        return False, "在 'data' 列表中沒有找到任何有效的回饋紀錄"

    try:
        log_df = pd.DataFrame(log_records)
        log_df.to_csv(
            FEEDBACK_FILE, 
            mode='a', 
            header=not os.path.exists(FEEDBACK_FILE), 
            index=False,
            quoting=csv.QUOTE_MINIMAL
        )
    except Exception as e:
        return False, f"寫入 feedback_log.csv 時發生錯誤: {repr(e)}"

    new_records_df = pd.DataFrame(new_records)
    success, message = update_model_with_feedback(new_records_df)
    if not success:
        return False, f"處理回饋時發生錯誤: {message}"
    return True, "模型更新成功!"