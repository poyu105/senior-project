export default function Datebar({date=new Date(), setDate, showPrevBtn=true, showNextBtn=true}){
    //日期轉換格式
    const formatDate = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');

        const weekdays = ["日", "一", "二", "三", "四", "五", "六"];
        const weekday = weekdays[date.getDay()];

        return `${year}/${month}/${day} (${weekday})`;
    }

    //改變日期
    const changeDate = (days) => {
        const newDate = new Date(date);
        newDate.setDate(newDate.getDate() + days);
        setDate(newDate);
    }

    const prevBtnVisible = typeof showPrevBtn === "function" ? showPrevBtn() : showPrevBtn;
    const nextBtnVisible = typeof showNextBtn === "function" ? showNextBtn() : showNextBtn;
    return(
        <>
            <div className="container col-md-6 bg-secondary bg-opacity-10 rounded-3 my-3">
                <div className="d-flex justify-content-between gap-3">
                    {/* 往前一天 */}
                    <button
                        className="btn btn-link text-dark d-flex align-items-center"
                        onClick={()=>{
                            changeDate(-1);
                        }}
                        disabled={!prevBtnVisible}>
                        <i className="bi bi-caret-left-fill fs-4"></i>
                    </button>
                    {/* 日期 */}
                    <h4 className="align-content-center m-0">{formatDate(date)}</h4>
                    {/* 往後一天 */}
                    <button
                        className="btn btn-link text-dark d-flex align-items-center"
                        onClick={()=>{
                            changeDate(1);
                        }}
                        disabled={!nextBtnVisible}>
                        <i className="bi bi-caret-right-fill fs-4"></i>
                </button>
                </div>
            </div>
        </>
    )
}