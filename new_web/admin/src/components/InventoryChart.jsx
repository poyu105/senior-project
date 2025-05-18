import Chart from 'react-apexcharts';
import './InventoryChart.css';

export default function InventoryChart({ data }){
    // 依據庫存決定顏色
    const getColorByQuantity = (qty) => {
        if (qty >= 100) return '#4CAF50';     
        if (qty >= 50) return '#FFA707';      
        return '#F44336';                     
    };

    const options = {
        chart:{
            type: 'bar',
            height: 350,
        },
        plotOptions:{
            bar:{
                horizontal: false,
                columnWidth: '50%',
                distributed: true,
            },
        },
        dataLabels:{
            enabled: true,
        },
        xaxis:{
            categories: data.map(item=>item.name),
            labels:{
                rotate: -45,
                style:{
                    fontSize: '12px',
                },
            },
        },
        yaxis:{
            title:{
                text: '庫存數量',
            },
        },
        title:{
            text: '商品庫存狀況',
            align: 'center',
        },
        colors: data.map(item=>getColorByQuantity(item.quantity)),
        legend: {
            show: false,
        },
    };

    const series = [
        {
            name: '庫存',
            data: data.map(item=>item.quantity),
        },
    ];

    return(
        <>
            <div style={{ height: 'calc(100vh - 235px)' }}>
                <Chart options={options} series={series} type="bar" height="100%" />
            </div>
            <div>
                <ul className="d-flex justify-content-center gap-2 list-unstyled m-0">
                    <li className="stock-high">庫存充足{'(>=100)'}</li>
                    <li className="stock-medium">庫存普通{'(50~99)'}</li>
                    <li className="stock-low">庫存不足{'(<50)'}</li>
                </ul>
            </div>
        </>
    )
}