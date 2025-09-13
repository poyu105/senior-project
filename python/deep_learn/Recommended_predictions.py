#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Recommended_predictions.py

1) 依歷史銷售（可含天氣）對每個口味建 baseline 模型（RandomForest）。
2) 輸出未來 N 天（預設 7）各口味預測 CSV（含粗略上下界）。
3) 依時間與素食偏好給出 Top-K 推薦。

預設已對應你的 Excel 欄位：
  --date-col order_date   --meal-col 泡麵_name   --qty-col quantity
需要的套件：pandas numpy scikit-learn openpyxl
"""

import argparse
import warnings
from datetime import datetime, timedelta
from pathlib import Path

import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestRegressor
from sklearn.model_selection import TimeSeriesSplit
from sklearn.metrics import mean_absolute_error

warnings.filterwarnings("ignore", category=UserWarning)

# --------------------------
# Helpers
# --------------------------
SEASONS = {
    12: "winter", 1: "winter", 2: "winter",
    3: "spring", 4: "spring", 5: "spring",
    6: "summer", 7: "summer", 8: "summer",
    9: "autumn", 10: "autumn", 11: "autumn",
}

def _season(month: int) -> str:
    return SEASONS.get(month, "unknown")

def _safe_lower(s: pd.Series, k: float = 1.25) -> pd.Series:
    return (s / k).clip(lower=0.0)

def _safe_upper(s: pd.Series, k: float = 0.75) -> pd.Series:
    return (s / k + 1).clip(lower=s)

# --------------------------
# Feature Engineering
# --------------------------
def make_features(df: pd.DataFrame, date_col: str, temp_col: str | None = None, group_cols: list[str] | None = None):
    """建立日曆/季節/lag 特徵；回傳含特徵的資料與特徵欄位名。"""
    if group_cols is None:
        group_cols = []

    df = df.copy()
    df[date_col] = pd.to_datetime(df[date_col])
    df["y"] = pd.to_numeric(df["y"], errors="coerce").fillna(0.0)

    # Calendar features
    df["dow"] = df[date_col].dt.weekday
    df["dom"] = df[date_col].dt.day
    df["week"] = df[date_col].dt.isocalendar().week.astype(int)
    df["month"] = df[date_col].dt.month
    df["season"] = df["month"].map(_season)
    df = pd.get_dummies(df, columns=["season"], drop_first=False)

    if "hour" not in df.columns:
        df["hour"] = 12  # neutral hour

    feature_cols = ["dow", "dom", "week", "month", "hour"] + [c for c in df.columns if c.startswith("season_")]
    if temp_col and temp_col in df.columns:
        df["temp"] = pd.to_numeric(df[temp_col], errors="coerce")
        df["temp"] = df["temp"].fillna(df["temp"].median())
        feature_cols.append("temp")

    # weekly lags per group
    if group_cols:
        df = df.sort_values(group_cols + [date_col])
        for lag in (7, 14, 21):
            col = f"lag_{lag}"
            df[col] = df.groupby(group_cols)["y"].shift(lag)
            feature_cols.append(col)

    # 訓練資料才需要把含 lag NaN 的列丟掉；未來資料另外處理
    return df, feature_cols

# --------------------------
# Model Training + Forecast
# --------------------------
def train_and_forecast(
    data: pd.DataFrame,
    date_col: str = "order_date",
    meal_col: str = "泡麵_name",
    qty_col: str = "quantity",
    temp_col: str | None = None,  # 例如 "AvgTemp"（若有）
    horizon_days: int = 7,
):
    # 標準化欄名
    df = data.rename(columns={date_col: "ds", meal_col: "meal", qty_col: "y"}).copy()
    if temp_col and temp_col in df.columns:
        df = df.rename(columns={temp_col: "AvgTemp"})
        temp_col = "AvgTemp"
    else:
        temp_col = None

    # 日粒度彙總
    df = df.groupby(["meal", "ds"], as_index=False).agg({"y": "sum", **({temp_col: "mean"} if temp_col else {})})
    last_date = df["ds"].max()
    horizon = [last_date + timedelta(days=i) for i in range(1, horizon_days + 1)]

    all_forecasts = []

    for meal, g in df.groupby("meal"):
        # 建訓練特徵（含 lag）
        g = g.sort_values("ds").copy()
        g["hour"] = 12
        feat_df, feat_cols = make_features(
            g.rename(columns={"ds": "Date"}), "Date", temp_col=temp_col, group_cols=["meal"]
        )
        # 丟掉 lag 還沒齊的列
        need_drop = [c for c in feat_cols if c.startswith("lag_")]
        if need_drop:
            feat_df = feat_df.dropna(subset=need_drop, how="any")

        if len(feat_df) < 30:
            # 資料不足：用簡單平均
            yhat = np.full(horizon_days, g["y"].mean())
            meal_fc = pd.DataFrame({"date": horizon, "meal": meal, "yhat": yhat})
            meal_fc["lower"] = _safe_lower(meal_fc["yhat"])
            meal_fc["upper"] = _safe_upper(meal_fc["yhat"])
            all_forecasts.append(meal_fc)
            continue

        X = feat_df[feat_cols].values
        y = feat_df["y"].values

        # 時序 CV（僅評估）
        tscv = TimeSeriesSplit(n_splits=min(5, max(2, len(feat_df) // 28)))
        cv_maes = []
        for tr, va in tscv.split(X):
            model = RandomForestRegressor(n_estimators=300, random_state=42, n_jobs=-1)
            model.fit(X[tr], y[tr])
            pred = model.predict(X[va])
            cv_maes.append(mean_absolute_error(y[va], pred))

        # 最終模型
        model = RandomForestRegressor(n_estimators=500, random_state=42, n_jobs=-1)
        model.fit(X, y)

        # -------- 構建未來特徵（關鍵：補齊 lag 與季節欄位） --------
        future = pd.DataFrame({"Date": horizon})
        future["meal"] = meal
        future["hour"] = 12
        if temp_col:
            last_temp = g[temp_col].dropna().iloc[-1] if g[temp_col].notna().any() else np.nan
            future[temp_col] = last_temp

        # 用「最近歷史 + 未來占位」一起算 lag
        recent = g.rename(columns={"ds": "Date"})[["Date", "meal", "y"]].sort_values("Date").tail(30)
        recent2 = recent.assign(hour=12).copy()
        if temp_col:
            # recent 只有在原本 g 含 temp_col 時才會有該欄
            if temp_col in g.columns:
                recent2[temp_col] = g[temp_col].tail(len(recent)).values
            else:
                recent2[temp_col] = np.nan

        future2 = future.assign(y=0.0).copy()
        tmp = pd.concat([recent2, future2], ignore_index=True).sort_values(["meal", "Date"])

        # 先補上日曆/季節 dummy
        tmp["dow"] = tmp["Date"].dt.weekday
        tmp["dom"] = tmp["Date"].dt.day
        tmp["week"] = tmp["Date"].dt.isocalendar().week.astype(int)
        tmp["month"] = tmp["Date"].dt.month
        tmp["season"] = tmp["month"].map(_season)
        tmp = pd.get_dummies(tmp, columns=["season"], drop_first=False)

        # 算 lag
        for lag in (7, 14, 21):
            tmp[f"lag_{lag}"] = tmp.groupby(["meal"])["y"].shift(lag)

        # 只取未來區段
        tmp_future = tmp[tmp["Date"].isin(horizon)].copy()

        # 讓未來特徵欄【完整對齊】訓練時的 feat_cols；缺的補 0
        for c in feat_cols:
            if c not in tmp_future.columns:
                tmp_future[c] = 0.0
        X_future = tmp_future[feat_cols].values

        yhat = model.predict(X_future)
        meal_fc = pd.DataFrame({
            "date": horizon,
            "meal": meal,
            "yhat": yhat,
            "cv_mae": np.mean(cv_maes) if cv_maes else np.nan,
        })
        meal_fc["lower"] = _safe_lower(meal_fc["yhat"])
        meal_fc["upper"] = _safe_upper(meal_fc["yhat"])
        all_forecasts.append(meal_fc)

    return pd.concat(all_forecasts, ignore_index=True)

# --------------------------
# Simple Recommendation
# --------------------------
TIME_BUCKETS = {"breakfast": (5, 10), "lunch": (11, 14), "dinner": (17, 21), "late": (21, 24)}

def infer_bucket(hour: int) -> str:
    for name, (lo, hi) in TIME_BUCKETS.items():
        if lo <= hour <= hi:
            return name
    return "other"

def apply_time_bias(df: pd.DataFrame, meal_col="meal", score_col="yhat", when: datetime | None = None):
    if when is None:
        return df.assign(score=df[score_col])

    hour = when.hour
    bucket = infer_bucket(hour)
    name = df[meal_col].astype(str).str
    boost = np.where(bucket == "breakfast", name.contains("海鮮") * 1.10 + (~name.contains("海鮮")) * 1.00,
             np.where(bucket == "lunch", name.contains("辣") * 1.10 + (~name.contains("辣")) * 1.00,
             np.where(bucket in ("dinner", "late"),
                      name.contains("清湯") * 1.10 + name.contains("牛") * 1.05 + (~(name.contains("清湯") | name.contains("牛"))) * 1.00,
                      1.00)))
    return df.assign(score=df[score_col] * boost)

def filter_vegetarian(df: pd.DataFrame, meal_col="meal", vegetarian=False):
    if not vegetarian:
        return df
    name = df[meal_col].astype(str)
    veg_mask = name.str.contains("素") | name.str.contains("蔬") | name.str.contains("菜")
    return df[veg_mask].copy()

def top_k_recommendations(fc: pd.DataFrame, when: datetime, k: int = 5, vegetarian: bool = False):
    d = when.date()
    candidates = fc[fc["date"] == pd.Timestamp(d)]
    if candidates.empty:
        candidates = fc[fc["date"] == fc["date"].min()]

    candidates = filter_vegetarian(candidates, vegetarian=vegetarian)
    candidates = apply_time_bias(candidates, when=when)
    return candidates.sort_values("score", ascending=False).head(k)[["meal", "yhat", "score", "lower", "upper"]]

# --------------------------
# CLI
# --------------------------
def main():
    ap = argparse.ArgumentParser()
    # 預設對應你的欄位
    ap.add_argument("--data", type=str, default="Final_Data_with_weather.xlsx")
    ap.add_argument("--date-col", type=str, default="order_date")
    ap.add_argument("--meal-col", type=str, default="泡麵_name")
    ap.add_argument("--qty-col",  type=str, default="quantity")
    ap.add_argument("--temp-col", type=str, default="AvgTemp")  # 若無此欄會自動忽略
    ap.add_argument("--horizon", type=int, default=7)
    ap.add_argument("--out", type=str, default="forecasts.csv")
    ap.add_argument("--recommend", type=int, default=0)
    ap.add_argument("--when", type=str, default="")
    ap.add_argument("--vegetarian", action="store_true")
    args = ap.parse_args()

    # 讀檔
    if args.data.lower().endswith((".xlsx", ".xls")):
        df = pd.read_excel(args.data)
    else:
        df = pd.read_csv(args.data, encoding="utf-8")

    required = {args.date_col, args.meal_col, args.qty_col}
    if not required.issubset(df.columns):
        raise ValueError(f"Missing columns: {required - set(df.columns)}.")

    fc = train_and_forecast(
        data=df,
        date_col=args.date_col,
        meal_col=args.meal_col,
        qty_col=args.qty_col,
        temp_col=args.temp_col if args.temp_col in df.columns else None,
        horizon_days=args.horizon,
    )

    out_path = Path(args.out).with_suffix(".csv")
    fc.to_csv(out_path, index=False, encoding="utf-8-sig")
    print(f"[OK] Wrote forecasts → {out_path.resolve()} (rows={len(fc)})")

    if args.recommend > 0:
        when = pd.to_datetime(args.when) if args.when else pd.Timestamp(datetime.now())
        topk = top_k_recommendations(fc, when=when, k=args.recommend, vegetarian=args.vegetarian)
        if topk.empty:
            print("\n[WARN] No candidates for recommendation.")
        else:
            print("\nTop-K recommendations")
            print(topk.to_string(index=False))

if __name__ == "__main__":
    main()
