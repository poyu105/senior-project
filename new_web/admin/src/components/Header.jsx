import { useEffect } from "react";
import { useLocation } from "react-router-dom";

export default function Header({setHeaderTitle, headerTitle, search, setSearch}){
    const location = useLocation(); //目前位置
    //變更標題
    useEffect(()=>{
        const titlemap = {
        "/login":"登入系統",
        "/register":"註冊用戶",
        "/":"首頁",
        "/inventory":"庫存管理",
        "/dailyReport":"每日報表",
        "/prediction":"銷售預測",
        };
        setHeaderTitle(titlemap[location.pathname]);
    },[location])
    return(
        <>
            <div className="d-flex align-items-center">
                <h1>{headerTitle}</h1>
                {headerTitle == "創建展會遊戲" || headerTitle == "修改遊戲" || headerTitle == "登入系統" || headerTitle == "註冊用戶" ? null : 
                    <div className="container col-4 d-flex justify-content-center gap-2">
                        <input 
                            type="text" 
                            className="p-1 border-secondary border rounded" 
                            value={search}
                            placeholder="輸入關鍵字搜尋" 
                            onChange={(e)=>setSearch(e.target.value)}/>
                        <button type="button" className="btn btn-outline-secondary" onClick={()=>setSearch('')}>清除</button>
                    </div>
                }
            </div>
            <hr/>
        </>
    )
}