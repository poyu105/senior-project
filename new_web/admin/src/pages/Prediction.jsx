import { useEffect, useState } from "react";
import SalesChart from "../components/SalesChart";
import ApiServices from "../api/ApiServices";
import Datebar from "../components/Datebar";
import { useLoading } from "../context/LoadingContext";

export default function Prediction(){
    const {loading, setLoading} = useLoading();

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

    const [predictionDate, setPredictionDate] = useState(new Date()); //預測日期(預設今天)
    const [predictionSales, setPredictionSales] = useState([]); //預測銷售資料
    //取得預測銷售資料
    useEffect(()=>{
        const fetchPredictionSales = async () => {
            setLoading(true);
            const year = predictionDate.getFullYear();
            const month = String(predictionDate.getMonth() + 1).padStart(2, "0"); // 月份要 +1
            const day = String(predictionDate.getDate()).padStart(2, "0");
            const formatted = `${year}-${month}-${day}`;
            const data = {
                date: formatted, //轉換成 YYYY-MM-DD 格式
                latitude: location.latitude, 
                longitude: location.longitude
            };
            console.log(data);
            const res = await ApiServices.getPrediction(data);
            if(res){
                setPredictionSales(res);
            }
            setLoading(false);
        };
        fetchPredictionSales();
    },[,predictionDate]);
    return(
        <>
            {/* 日期 */}
            <Datebar 
                date={predictionDate} 
                setDate={setPredictionDate} 
                showPrevBtn={()=>{
                    const today = new Date();
                    today.setHours(0,0,0,0);
                    predictionDate.setHours(0,0,0,0);
                    return predictionDate > new Date();
                }}
                showNextBtn={()=>{
                    const maxDate = new Date();
                    maxDate.setDate(maxDate.getDate() + 2);
                    maxDate.setHours(0,0,0,0);
                    predictionDate.setHours(0,0,0,0);
                    return predictionDate < maxDate;
                }} />
            
            {/* 預測銷售資料 */}
            <SalesChart title={"銷售預測資料"} data={predictionSales} />
        </>
    )
}