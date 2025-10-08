import { useEffect, useState } from "react";
import Datebar from "../components/Datebar";

export default function DailyReport(){
    const [date, setDate] = useState(new Date()); //日期(預設今天)

    //取得使用者位置
    const [location, setLocation] = useState({ latitude: 25.0478, longitude: 121.5319 });
    useEffect(() => {
        if (navigator.geolocation) {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    setLocation({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                    console.log(position.coords.latitude, position.coords.longitude);
                },
                (error) => {
                    console.error(error);
                    //台北預設座標
                    setLocation({ latitude: 25.0478, longitude: 121.5319 });
                }
            );
        } else {
            console.error("瀏覽器不支援 Geolocation API");
        }
    }, []);

    return(
        <>
            <Datebar 
                date={date} 
                setDate={setDate} 
                showNextBtn={()=>{
                    const today = new Date();
                    today.setHours(0,0,0,0);
                    return date < today;
                }} 
            />
            <div className="container">
                {/* 當前位置&刷新按鈕 */}
                <div className="d-flex justify-content-between align-items-end mb-2 border-bottom">
                    <p
                        className="text-secondary d-inline-block m-0"
                    >
                        當前位置: 經度 {location.longitude}, 緯度 {location.latitude}
                    </p>
                    <button
                        className="btn btn-success p-1 mb-1"
                    >
                        刷新報表
                    </button>
                </div>
                {/* 主要內容區 */}
                <div style={{ display: "flex", flexDirection: "column", height: "63vh" }}>
                    {/* 可滾動區域 */}
                    <div style={{ flex: 1, overflowY: "auto" }}>
                        <table 
                            className="table table-bordered text-center table-hover align-middle mb-0"
                            style={{ borderCollapse: "collapse", width: "100%" }}
                        >
                        <thead className="table-light" style={{ position: "sticky", top: 0, zIndex: 1 }}>
                            <tr>
                                <th>商品名稱</th>
                                <th>單價</th>
                                <th>今日銷售數量(非即時)</th>
                                <th>銷售金額</th>
                            </tr>
                        </thead>
                        <tbody>
                            {Array.from({ length: 20 }).map((_, i) => (
                            <tr key={i}>
                                <td>範例商品B</td>
                                <td>400</td>
                                <td
                                    style={{width: "200px"}}
                                >
                                    <input
                                        id={`salesAmountInput-${i}`}
                                        type="number"
                                        className="form-control text-center"
                                        style={{lineHeight: "1"}}
                                        defaultValue={0} 
                                        min={0}
                                        readOnly={date < new Date(new Date().setHours(0,0,0,0))} //過去日期不可編輯
                                    />
                                </td>
                                <td>
                                    0
                                </td>
                            </tr>
                            ))}
                        </tbody>
                        </table>
                    </div>

                    {/* 統計區塊 */}
                    <table className="table table-bordered text-center mb-0">
                        <tbody>
                            <tr className="table-secondary">
                                <th>總銷售數量</th>
                                <td>22</td>
                            </tr>
                            <tr className="table-secondary">
                                <th>總銷售金額</th>
                                <td>8000</td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                {/* 儲存按鈕 */}
                <div 
                    className="text-center mt-2"
                    style={{visibility: date < new Date(new Date().setHours(0,0,0,0)) ? "hidden" : "visible"}} //過去日期不顯示
                >
                    <button
                        className="btn btn-primary"
                        disabled={date < new Date(new Date().setHours(0,0,0,0))} //過去日期不可編輯
                    >
                        儲存
                    </button>
                </div>
            </div>
        </>
    )
}