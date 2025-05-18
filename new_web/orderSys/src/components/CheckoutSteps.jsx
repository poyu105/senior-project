import "./CheckoutSteps.css";

export default function CheckoutSteps({ currentStep }){
    return(
        <>
            <div className="container d-flex flex-column align-items-center mb-5">
                <h3>結帳流程</h3>
                {/* 進度條 */}
                <div className="position-relative py-3 w-50">
                    {/* 底線 */}
                    <div className="progress" style={{height: "2px"}}>
                        <div className="progress-bar" style={{width: `${currentStep/2*100}%`}}>
                        </div>
                    </div>
                    {/* 步驟1 */}
                    <div className={`step start-0 ${currentStep >= 0 ? "bg-primary" : "bg-secondary"}`}>
                        <h5 className="text-center text-light m-0">1</h5>
                        <span className="position-absolute top-100 start-50 translate-middle text-nowrap mt-3 pt-1">確認餐點</span>
                    </div>
                    {/* 步驟2 */}
                    <div className={`step start-50 ${currentStep >= 1 ? "bg-primary" : "bg-secondary"}`}>
                        <h5 className="text-center text-light m-0">2</h5>
                        <span className="position-absolute top-100 start-50 translate-middle text-nowrap mt-3 pt-1">選擇付款方式</span>
                    </div>
                    {/* 步驟3 */}
                    <div className={`step start-100 ${currentStep >= 2 ? "bg-primary" : "bg-secondary"}`}>
                        <h5 className="text-center text-light m-0">3</h5>
                        <span className="position-absolute top-100 start-50 translate-middle text-nowrap mt-3 pt-1">完成訂單</span>
                    </div>
                </div>
            </div>
        </>
    )
}