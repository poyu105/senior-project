import { useEffect, useState } from "react"
import { useLoading } from "../context/LoadingContext";
import ApiServices from "../api/ApiServices";
import SalesChart from "../components/SalesChart";

export default function Home(){
    const { setLoading } = useLoading();
    const [sales, setSales] = useState([]); //銷售狀況資料

    //取得銷售狀況資料
    useEffect(()=>{
        //取得銷售資料API
        const fetchSales = async ()=>{
            try {
                setLoading(true);
                const res = await ApiServices.getSales();
                if(res.success){
                    setSales(res.data);
                }else{
                    alert(`取得銷售資料失敗:${res.message}`);
                }
            } catch (error) {
                console.error(`取得銷售資料失敗:${error}`);
                alert(`取得銷售資料失敗${error}`);
            } finally {
                setLoading(false);
            }
        }

        fetchSales();
    },[]);

    return(
        <>
            <SalesChart title={"商品銷售狀況"} data={sales} />
        </>
    )
}