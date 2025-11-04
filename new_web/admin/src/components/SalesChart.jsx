import Chart from 'react-apexcharts';

export default function SalesChart({ title, data }) {
    const options = {
        chart: {
            type: 'bar',
            height: 350,
            toolbar: {
                show: false,
            },
        },
        plotOptions: {
            bar: {
                horizontal: false,
                columnWidth: '50%',
                distributed: true,
            },
            
        },
        stroke:{
            show: true,
            width: 1.5,
        },
        dataLabels: {
            enabled: true,
        },
        xaxis: {
            categories: data.map(item => item.meal_name || item.name),
            labels: {
                rotate: -45,
                style: {
                    fontSize: '12px',
                },
            },
        },
        yaxis: [
            {
                title: {
                    text: '銷售數量',
                },
            },
            {
                opposite: true,
                title: {
                    text: '銷售額',
                },
            }
        ],
        title: {
            text: title,
            align: 'center',
        },
        //每個bar使用不同顏色
        colors: [
            '#F44336', '#E91E63', '#9C27B0',
            '#3F51B5', '#2196F3', '#03A9F4',
            '#009688', '#4CAF50', '#FF9800',
            '#795548', '#607D8B'
        ],
        legend: {
            show: false,
        },
    };

    const series = [
        {
            name: '銷售數量',
            type: 'bar',
            data: data.map(item => item.amount ?? 0),
        },
        {
            name: '銷售額',
            type: 'line',
            data: data.map(item => item.sales ?? 0),
        },
        {
            name: '單價',
            type: 'none',
            data: data.map(item => item.cost ?? 0),
        },
    ];

    return (
        <div style={{ height: 'calc(100vh - 235px)' }} className='mt-3'>
            <Chart options={options} series={series} type="line" height="100%" />
        </div>
    );
}