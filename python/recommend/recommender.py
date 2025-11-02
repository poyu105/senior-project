import pandas as pd

def generate_recommendations(data: dict) -> list:

    try:
        # 1. 獲取當前天氣狀況
        current_weather = data.get('weather_condition')
        
        # 2. 獲取歷史訂單並轉換為 DataFrame
        orders = data.get('orders', [])
        if not orders:
            return [] 

        df = pd.DataFrame(orders)

        # 3. 根據情境篩選
        weather_matched_orders = df[df['weather_condition'] == current_weather]

        # 4. 若無符合情境的資料，則使用全部資料
        if weather_matched_orders.empty:
            df_to_analyze = df 
        else:
            df_to_analyze = weather_matched_orders 

        # 5. 計算
        popular_items = df_to_analyze.groupby(
            ['meal_id', 'name', 'type']
        )['total_amount'].sum()
        
        # 6. 銷量最高5個餐點
        top_5_series = popular_items.nlargest(5)

        if top_5_series.empty:
            return []

        # 7. 輸出
        recommend_df = top_5_series.reset_index()
        recommend_df['rank'] = (recommend_df.index + 1).astype(str)
        recommend_df = recommend_df.drop(columns=['total_amount'])
        final_columns = ['meal_id', 'name', 'rank', 'type']
        recommend_df = recommend_df[final_columns]
            
        # 8. 回傳字典列表
        return recommend_df.to_dict('records')

    except Exception as e:
        print(f"推薦餐點時發生錯誤: {e}")
        raise