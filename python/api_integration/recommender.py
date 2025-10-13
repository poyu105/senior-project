# recommender.py
from __future__ import annotations
import json
from datetime import datetime
from typing import List, Dict, Any, Optional
import numpy as np
import pandas as pd

TIME_BUCKETS = {"breakfast": (5,10), "lunch": (11,14), "dinner": (17,21), "late": (21,24)}

def infer_bucket(h:int)->str:
    for name,(lo,hi) in TIME_BUCKETS.items():
        if lo <= h <= hi: return name
    return "other"

def time_bias_multiplier(meal_name:str, meal_type:str, when:datetime)->float:
    meal_name = str(meal_name or "")
    meal_type = (meal_type or "").lower()
    bucket = infer_bucket(when.hour)
    if bucket=="breakfast":
        if "海鮮" in meal_name or meal_type=="seafood": return 1.10
    elif bucket=="lunch":
        if "辣" in meal_name or meal_type=="spicy": return 1.10
    elif bucket in ("dinner","late"):
        m=1.0
        if "清湯" in meal_name: m*=1.10
        if "牛" in meal_name:   m*=1.05
        return m
    return 1.0

def normalize_payload_to_df(payload: List[Dict[str, Any]]) -> pd.DataFrame:
    rows = []
    for day in (payload or []):
        day_date = day.get("date")
        for o in (day.get("orders") or []):
            od = str(o.get("order_date",""))
            if len(od)==8 and od.isdigit():
                dt = datetime.strptime(od, "%Y%m%d").date()
            elif day_date:
                dt = datetime.strptime(day_date, "%Y-%m-%d").date()
            else:
                continue
            rows.append({
                "date": pd.Timestamp(dt),
                "meal_id": o.get("meal_id"),
                "name": o.get("name"),
                "type": (o.get("type") or "").lower(),
                "qty": float(o.get("total_amount",0) or 0),
            })
    if not rows:
        return pd.DataFrame(columns=["date","meal_id","name","type","qty"])
    df = pd.DataFrame(rows)
    return df.groupby(["date","meal_id","name","type"], as_index=False)["qty"].sum()

def get_user_last_n_orders(payload, user:str, n:int=5) -> List[Dict[str,Any]]:
    rows=[]
    for day in (payload or []):
        for o in (day.get("orders") or []):
            if str(o.get("user_id"))!=str(user):
                continue
            od = str(o.get("order_date",""))
            if len(od)==8 and od.isdigit():
                dt = datetime.strptime(od,"%Y%m%d")
            else:
                try:
                    dt = datetime.strptime(day.get("date",""), "%Y-%m-%d")
                except Exception:
                    continue
            rows.append({
                "order_dt": dt,
                "meal_id": o.get("meal_id"),
                "name": o.get("name"),
                "type": (o.get("type") or "").lower(),
                "total_amount": float(o.get("total_amount",0) or 0),
            })
    if not rows: return []
    df = pd.DataFrame(rows).sort_values("order_dt", ascending=False).head(n)
    uniq, seen = [], set()
    for _,r in df.iterrows():
        mid = str(r["meal_id"])
        if mid in seen:
            continue
        seen.add(mid)
        uniq.append({
            "order_date": r["order_dt"].strftime("%Y%m%d"),
            "meal_id": mid,
            "name": str(r["name"]),
            "type": str(r["type"]),
            "total_amount": float(r["total_amount"]),
        })
    return uniq

def score_recent_popularity(df:pd.DataFrame, ref_date:pd.Timestamp, window_days:int=14, lambda_decay:float=0.25)->pd.DataFrame:
    start = ref_date - pd.Timedelta(days=window_days)
    sub = df[(df["date"]<=ref_date) & (df["date"]>start)].copy()
    if sub.empty: sub = df.copy()
    sub["days_ago"] = (ref_date - sub["date"]).dt.days.clip(lower=0)
    sub["w"] = np.exp(-lambda_decay * sub["days_ago"])
    g = (sub.groupby(["meal_id","name","type"], as_index=False)
           .apply(lambda x: pd.Series({
               "score_base": float((x["qty"]*x["w"]).sum() / max(x["w"].sum(),1e-6)),
               "qty_sum": float(x["qty"].sum()),
               "days_covered": int(x["date"].dt.normalize().nunique())
           }))
           .reset_index(drop=True))
    return g

def apply_business_bias(g:pd.DataFrame, when:datetime)->pd.DataFrame:
    g = g.copy()
    g["bias"]  = g.apply(lambda r: time_bias_multiplier(r["name"], r["type"], when), axis=1)
    g["score"] = g["score_base"] * g["bias"]
    return g

def recommend_from_payload(payload: List[Dict[str,Any]], user:str="guest", when:Optional[str]=None, topk:int=5, window:int=14):
    when_ts = pd.Timestamp(when) if when else pd.Timestamp(datetime.now())
    # 先用個人最近 5 筆
    selected, used = [], set()
    for r in get_user_last_n_orders(payload, user=user, n=5):
        mid = r["meal_id"]
        if mid and mid not in used:
            used.add(mid)
            selected.append({"meal_id":mid, "name":r["name"], "type":r["type"]})
        if len(selected)>=topk: break
    # 不足再用熱門度補
    if len(selected)<topk:
        df = normalize_payload_to_df(payload)
        if not df.empty:
            g  = score_recent_popularity(df, ref_date=when_ts.normalize(), window_days=window, lambda_decay=0.25)
            g2 = apply_business_bias(g, when=when_ts.to_pydatetime())
            for _,r in g2.sort_values("score", ascending=False).iterrows():
                mid = str(r["meal_id"])
                if mid and mid not in used:
                    used.add(mid)
                    selected.append({"meal_id":mid, "name":str(r["name"]), "type":str(r["type"])})
                if len(selected)>=topk: break
    # 組長要的格式
    orders = [{"rank":str(i+1), "meal_id":s["meal_id"], "name":s["name"], "type":s["type"]}
              for i,s in enumerate(selected[:topk])]
    return [{"orders": orders}]
