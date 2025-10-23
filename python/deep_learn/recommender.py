#!/usr/bin/env python3
# -*- coding: utf-8 -*-

"""
Deep Learning Recommender (External-only, robust preproc + required cols)
- 強制使用外掛模型：dlrec_model.h5 + dlrec_preproc.pkl
- 相容：preproc 可能是 sklearn 物件或 dict（含 'preprocessor'/'pipeline' 等）
- 會自動產生並補齊模型常用欄位：hour,dow,month,is_weekend,week,temp,
  qty_sum_{1,3,5,7,14}d, count_{1,3,5,7,14}d, user_recent_{1,3,5,7,14}d,
  lag_1..lag_5, weather_condition, season, is_veg
- API 介面維持：
    recommend_from_payload(payload, user="guest", when=None, topk=5, window=14)
    -> [{"orders":[{"rank":"1","meal_id":"...","name":"...","type":"..."}, ...]}]
"""

from __future__ import annotations

import os
from typing import Dict, Any, List, Optional, Tuple

import numpy as np
import pandas as pd

# ---- TensorFlow/Keras ----
try:
    import tensorflow as tf  # noqa: F401
    from tensorflow.keras.models import load_model as _ext_load_model
except Exception as e:
    raise RuntimeError(
        "本程式需要 TensorFlow/Keras，請先安裝：pip install \"tensorflow==2.15.*\""
    ) from e

# ---- 外掛模型（強制） ----
EXT_DL_READY = False
EXT_DL_ERR = None
_FEATURE_ORDER: List[str] = []  # 由 preproc 提供的特徵順序（若有）

try:
    import joblib
    _EXT_MODEL = _ext_load_model(os.path.join(os.getcwd(), "dlrec_model.h5"))
    _EXT_PREP  = joblib.load(os.path.join(os.getcwd(), "dlrec_preproc.pkl"))

    # 嘗試從物件抓欄位順序
    for key in ("feature_order", "input_features_", "feature_names_in_"):
        if hasattr(_EXT_PREP, key):
            _FEATURE_ORDER = list(getattr(_EXT_PREP, key))  # type: ignore
            break
    if hasattr(_EXT_PREP, "get_feature_names_out") and not _FEATURE_ORDER:
        try:
            _FEATURE_ORDER = list(_EXT_PREP.get_feature_names_out())  # type: ignore
        except Exception:
            pass

    EXT_DL_READY = True
except Exception as _e:
    EXT_DL_READY = False
    EXT_DL_ERR = _e


# ====================== 工具 ======================

def _to_ts(x) -> pd.Timestamp:
    return pd.to_datetime(x, errors="coerce").normalize()

def _dow(dt: pd.Timestamp) -> int:
    return int(dt.weekday()) if pd.notna(dt) else 0


# ====================== Payload 解析 ======================

def _payload_to_df(payload: List[Dict[str, Any]]) -> pd.DataFrame:
    """
    將「日彙整」payload 轉成 DataFrame
    期望格式：
    [
      {
        "date": "YYYY-MM-DD",
        "weather_condition": "S",
        "season": "su",
        "AvgTemp": 28.5,  # 或 temperature / temp
        "orders": [
          {"order_date":"20250920","user_id":"guest","meal_id":"m1","name":"辛辣麵","type":"spicy","total_amount":3}
        ]
      }, ...
    ]
    """
    rows = []
    for day in payload or []:
        date_str = day.get("date")
        dt = _to_ts(date_str) if date_str else None
        w = str(day.get("weather_condition", "NA"))

        season = day.get("season")
        season_map = {"su": "summer", "au": "autumn", "wi": "winter", "sp": "spring"}
        season_full = season_map.get(str(season).lower(), str(season).lower() if season else "summer")

        temp_val = None
        for c in ("AvgTemp", "temperature", "temp"):
            if c in day:
                try:
                    temp_val = float(day[c])
                except Exception:
                    temp_val = None
                break

        for o in day.get("orders", []) or []:
            u = str(o.get("user_id", "guest"))
            m = str(o.get("meal_id", o.get("name", "")))
            nm = str(o.get("name", m))
            t = str(o.get("type", "other")).lower()
            qty = float(o.get("total_amount", 1) or 1)
            rows.append(
                {
                    "date": dt,
                    "user_id": u,
                    "meal_id": m,
                    "name": nm,
                    "type": t,
                    "quantity": qty,
                    "weather_condition": w,
                    "temp": temp_val if temp_val is not None else 0.0,
                    "dow": _dow(dt) if dt is not None else 0,
                    "season": season_full,
                }
            )
    if not rows:
        return pd.DataFrame(columns=["date","user_id","meal_id","name","type","quantity","weather_condition","temp","dow","season"])
    return pd.DataFrame(rows)


# ====================== preproc 適配器 ======================

def _ext_find_transformer(prep):
    """
    回傳具有 .transform 的實體；支援：
    - 直接是 transformer
    - dict 內常見 key：'preprocessor','preproc','pipeline','pipe',
                        'vectorizer','encoder','scaler'
    找不到則回 None（代表不做 transform，直接餵數值矩陣）
    """
    if hasattr(prep, "transform"):
        return prep
    if isinstance(prep, dict):
        for key in ("preprocessor", "preproc", "pipeline", "pipe", "vectorizer", "encoder", "scaler"):
            obj = prep.get(key)
            if hasattr(obj, "transform"):
                return obj
    return None

def _ext_feature_order_from(prep) -> List[str]:
    """
    取得特徵順序；優先順序：
    - 全域 _FEATURE_ORDER
    - 物件屬性：feature_order / input_features_ / feature_names_in_
    - dict key：'feature_order' / 'features' / 'feature_names_in_'
    找不到則回空清單（表示用原表欄位）
    """
    if _FEATURE_ORDER:
        return list(_FEATURE_ORDER)
    for key in ("feature_order", "input_features_", "feature_names_in_"):
        if hasattr(prep, key):
            try:
                return list(getattr(prep, key))
            except Exception:
                pass
    if hasattr(prep, "get_feature_names_out"):
        try:
            return list(prep.get_feature_names_out())
        except Exception:
            pass
    if isinstance(prep, dict):
        for key in ("feature_order", "features", "feature_names_in_"):
            if key in prep:
                try:
                    return list(prep[key])
                except Exception:
                    pass
    return []


# ====================== 特徵工程 ======================

def _ext_parse_when(when: Optional[str], fallback: pd.Timestamp) -> pd.Timestamp:
    if not when:
        return fallback
    try:
        return pd.to_datetime(when)
    except Exception:
        return fallback

def _ext_build_candidates_from_payload(df_hist: pd.DataFrame) -> pd.DataFrame:
    keep = [c for c in ("meal_id", "name", "type") if c in df_hist.columns]
    if not keep or df_hist.empty:
        return pd.DataFrame(columns=["meal_id","name","type"])
    return df_hist[keep].drop_duplicates(subset=["meal_id"]).reset_index(drop=True)

def _ext_make_feature_table(df_hist: pd.DataFrame, when_ts: pd.Timestamp) -> pd.DataFrame:
    """
    產生模型常用特徵，並補齊 preproc 需要的欄位：
      - 時間特徵：hour, dow, month, is_weekend, week
      - 天氣/季節：weather_condition, season
      - 溫度：temp（若 payload 有）
      - 近 1/3/5/7/14 天：qty_sum_*d / count_*d / user_recent_*d
      - 逐日滯後：lag_1..lag_5（各餐點在 D-1...D-5 的數量；缺則 0）
      - 類別旗標：is_veg
    """
    cand = _ext_build_candidates_from_payload(df_hist)
    if cand.empty:
        return cand

    # === 當天 context ===
    last_day = df_hist["date"].max()
    ctx = df_hist.loc[df_hist["date"] == last_day].head(1)
    weather = str(ctx["weather_condition"].iloc[0]) if "weather_condition" in ctx.columns and len(ctx) else "NA"
    season_val = str(ctx["season"].iloc[0]) if "season" in ctx.columns and len(ctx) else "summer"

    # === 時間特徵 ===
    cand["hour"] = when_ts.hour
    cand["dow"] = when_ts.weekday()
    cand["month"] = when_ts.month
    cand["is_weekend"] = 1 if cand["dow"].iloc[0] >= 5 else 0
    try:
        # pandas >=1.1: isocalendar() 回傳 DataFrame/命名tuple
        cand["week"] = int(getattr(when_ts.isocalendar(), "week", when_ts.week))
    except Exception:
        cand["week"] = int(getattr(when_ts.isocalendar(), "week", 0))

    # === 天氣/季節 ===
    cand["weather_condition"] = weather
    cand["season"] = season_val

    # === 溫度（取最後一天） ===
    temp_val = None
    if "temp" in ctx.columns and len(ctx):
        try:
            temp_val = float(ctx["temp"].iloc[0])
        except Exception:
            temp_val = None
    cand["temp"] = temp_val

    # === 近 x 天統計 ===
    have_ts = "date" in df_hist.columns and df_hist["date"].notna().any() and "meal_id" in df_hist.columns
    if have_ts:
        for win in (1, 3, 5, 7, 14):
            start = (when_ts.normalize() - pd.Timedelta(days=win))
            mask = (df_hist["date"] < when_ts.normalize()) & (df_hist["date"] >= start)
            grp = (
                df_hist.loc[mask]
                .groupby("meal_id")["quantity"]
                .agg(["sum","count"])
                .rename(columns={"sum":f"qty_sum_{win}d","count":f"count_{win}d"})
                .reset_index()
            )
            cand = cand.merge(grp, on="meal_id", how="left")
            cand[f"qty_sum_{win}d"] = cand[f"qty_sum_{win}d"].fillna(0.0)
            cand[f"count_{win}d"]   = cand[f"count_{win}d"].fillna(0.0)
            cand[f"user_recent_{win}d"] = (cand[f"count_{win}d"] > 0).astype(int)
    else:
        for win in (1, 3, 5, 7, 14):
            cand[f"qty_sum_{win}d"] = 0.0
            cand[f"count_{win}d"] = 0.0
            cand[f"user_recent_{win}d"] = 0

    # === 逐日滯後 lag_1..lag_5（各餐點 D-1 到 D-5 的訂購量） ===
    for lag in (1, 2, 3, 4, 5):
        day = (when_ts.normalize() - pd.Timedelta(days=lag))
        q = (
            df_hist.loc[df_hist["date"] == day]
            .groupby("meal_id")["quantity"]
            .sum()
            .rename(f"lag_{lag}")
            .reset_index()
        )
        cand = cand.merge(q, on="meal_id", how="left")
        cand[f"lag_{lag}"] = cand[f"lag_{lag}"].fillna(0.0)

    # === 類別旗標 ===
    cand["is_veg"] = cand.get("type", pd.Series(dtype=object)).astype(str).str.contains("素", na=False).astype(int)
    return cand


# ====================== 推論主流程 ======================

def _ext_predict_scores_from_payload(payload: List[Dict[str, Any]], when: Optional[str]) -> List[Tuple[str, float, Dict[str, Any]]]:
    """
    回 [(meal_id, yhat, meta), ...]；若失敗回空清單
    """
    if not EXT_DL_READY:
        return []

    df_hist = _payload_to_df(payload)
    if df_hist.empty:
        return []

    last_day = df_hist["date"].max()
    when_ts = _ext_parse_when(when, fallback=last_day)

    feat_df = _ext_make_feature_table(df_hist, when_ts)
    if feat_df.empty:
        return []

    # === 對齊特徵欄位（若 preproc 有定義順序） ===
    feat_order = _ext_feature_order_from(_EXT_PREP)
    if feat_order:
        X = pd.DataFrame(index=feat_df.index)
        for c in feat_order:
            X[c] = feat_df[c] if c in feat_df.columns else 0
        X = X[feat_order]
    else:
        X = feat_df

    # === 取得真正的 transformer（兼容 dict）並做 transform ===
    TRANS = _ext_find_transformer(_EXT_PREP)

    # 從 transformer 嘗試抓「它期望的欄位集合」
    required_cols: List[str] = []
    if TRANS is not None and hasattr(TRANS, "feature_names_in_"):
        try:
            required_cols = list(getattr(TRANS, "feature_names_in_"))
        except Exception:
            required_cols = []

    # 若知道它要哪些欄位，先把缺的補 0（避免 ColumnTransformer 抱怨缺欄）
    if required_cols:
        for c in required_cols:
            if c not in X.columns:
                X[c] = 0
        X = X[required_cols]

    if TRANS is not None:
        try:
            Xp = TRANS.transform(X)
        except Exception:
            # 有些 transformer 需要原始欄名；也補齊一次再試
            X2 = feat_df.copy()
            if required_cols:
                for c in required_cols:
                    if c not in X2.columns:
                        X2[c] = 0
                X2 = X2[required_cols]
            Xp = TRANS.transform(X2)
    else:
        # preproc 只是 dict/metadata：不變換
        Xp = X.values if hasattr(X, "values") else np.asarray(X)

    # === 推論 ===
    yhat = _EXT_MODEL.predict(Xp, verbose=0).reshape(-1)

    meta_cols = [c for c in ("meal_id", "name", "type") if c in feat_df.columns]
    meta = feat_df[meta_cols].copy()

    out: List[Tuple[str, float, Dict[str, Any]]] = []
    for i in range(len(feat_df)):
        mid = str(meta.iloc[i]["meal_id"]) if "meal_id" in meta.columns else f"#{i}"
        info = {k: meta.iloc[i][k] for k in meta_cols}
        out.append((mid, float(yhat[i]), info))
    out.sort(key=lambda x: x[1], reverse=True)
    return out


# ====================== 對外 API 函式 ======================

def recommend_from_payload(
    payload: List[Dict[str, Any]],
    user: str = "guest",
    when: Optional[str] = None,
    topk: int = 5,
    window: int = 14,  # 保留參數但此版未使用
) -> List[Dict[str, Any]]:
    """
    只用外掛 DL 模型；模型/前處理器缺失 → 直接拋錯。
    回傳：
    [{"orders":[{"rank":"1","meal_id":"...","name":"...","type":"..."}, ...]}]
    """
    if not EXT_DL_READY:
        raise RuntimeError(
            "外掛 DL 模型不可用：請確認同目錄存在 dlrec_model.h5 與 dlrec_preproc.pkl。\n"
            f"載入錯誤：{repr(EXT_DL_ERR)}"
        )

    scored = _ext_predict_scores_from_payload(payload, when)
    if not scored:
        raise ValueError("payload 內容不足（無有效候選/特徵），無法產生推薦。請檢查陣列結構、date、orders[].meal_id/type/total_amount 等欄位。")

    orders: List[Dict[str, Any]] = []
    for i, (_, _, info) in enumerate(scored[:topk], start=1):
        orders.append({
            "rank": str(i),
            "meal_id": str(info.get("meal_id", "")),
            "name": str(info.get("name", "")),
            "type": str(info.get("type", "")),
        })
    return [{"orders": orders}]


# ====================== CLI Demo（可選） ======================

if __name__ == "__main__":
    import argparse, json
    ap = argparse.ArgumentParser(description="DL Recommender (External-only, robust preproc + required cols)")
    ap.add_argument("--demo", action="store_true", help="用本地 payload.json 示範輸出組長格式")
    ap.add_argument("--payload", type=str, default="orders_payload.json", help="demo 模式用的 payload 檔名")
    ap.add_argument("--user", type=str, default="guest")
    ap.add_argument("--topk", type=int, default=5)
    ap.add_argument("--when", type=str, default="")
    args = ap.parse_args()

    if args.demo:
        p = args.payload
        if not os.path.exists(p):
            raise SystemExit(f"找不到 {p}")
        with open(p, "r", encoding="utf-8") as f:
            payload = json.load(f)
        res = recommend_from_payload(payload, user=args.user, when=(args.when or None), topk=args.topk)
        print(json.dumps(res, ensure_ascii=False, indent=2))
    else:
        ap.print_help()
