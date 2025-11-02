import { useEffect, useState } from "react";
import Datebar from "../components/Datebar";
import ApiServices from "../api/ApiServices";
import { useLoading } from "../context/LoadingContext";

export default function DailyReport(){
    const {loading, setLoading} = useLoading();
    const [date, setDate] = useState(new Date()); //日期(預設今天)
    const canEdit = date >= new Date(new Date().setHours(0,0,0,0)); //是否可編輯(今天或未來)
    //日期轉換格式
    const formatDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}/${month}/${day}`;
    }

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

    const [reportData, setReportData] = useState([]); //報表資料
    //取得報表資料
    const getReportData = async (date)=>{
        setLoading(true);
        const response = await ApiServices.getReportData(formatDate(date));
        if(response){
            setReportData(response.data);
        }
        setLoading(false);
    }
    //取得報表資料(初次渲染or日期改變)
    useEffect(()=>{
        getReportData(date);
    }, [date]);

    //儲存報表並更新模型
    const handleSubmit = async ()=>{
        try {
            setLoading(true);
            const data = {
                date: formatDate(date),
                latitude: location?.latitude,
                longitude: location?.longitude,
                data: reportData,
            }
            const res = await ApiServices.saveReports(data);

            if(res){
                alert(res.message);
            }
        } catch (error) {
            alert(`發生錯誤: ${error}`);
            console.error(`執行儲存報表時發生錯誤: ${error}`);
        } finally{
            setLoading(false);
        }
    }

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
                        onClick={()=>{
                            getReportData(date);
                        }}
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
                        {/* 表格輸入 */}
                        <tbody>
                            {reportData.map((d, i) => (
                            <tr key={i}>
                                {/* 餐點名稱 */}
                                <td>{d.name}</td>
                                {/* 單價 */}
                                <td>{d.cost}</td>
                                {/* 今日銷售數量 */}
                                <td
                                    style={{width: "200px"}}
                                >
                                    {
                                        !canEdit ? //過去日期不可編輯
                                        <span className="text-secondary">{d.amount}</span> :
                                        <input
                                            id={`salesAmountInput-${i}`}
                                            type="number"
                                            className="form-control text-center"
                                            style={{lineHeight: "1"}}
                                            value={d.amount} 
                                            min={0}
                                            readOnly={!canEdit} //過去日期不可編輯
                                            onChange={(e)=>{
                                                const value = e.target.value;
                                                if(value === "" || isNaN(value) || parseInt(value) < 0){
                                                    return;
                                                }else{
                                                    const newReportData = [...reportData];
                                                    newReportData[i].amount = parseInt(value);
                                                    newReportData[i].sales = newReportData[i].cost * parseInt(value);
                                                    setReportData(newReportData);
                                                }
                                            }}
                                        />
                                    }
                                </td>
                                {/* 銷售金額 */}
                                <td>
                                    {d.sales}
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
                                <td>
                                    {reportData.reduce((sum, item) => sum + item.amount, 0)}
                                </td>
                            </tr>
                            <tr className="table-secondary">
                                <th>總銷售金額</th>
                                <td>
                                    {reportData.reduce((sum, item) => sum + item.sales, 0)}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                {/* 儲存按鈕 */}
                <div 
                    className="text-center mt-2"
                    style={{visibility: canEdit ? "visible" : "hidden"}} //過去日期不顯示
                >
                    <button
                        className="btn btn-primary"
                        disabled={!canEdit} //過去日期不可編輯
                        onClick={()=>{
                            handleSubmit();
                        }}
                    >
                        儲存
                    </button>
                </div>
            </div>
        </>
    )
}