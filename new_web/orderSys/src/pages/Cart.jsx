import { useEffect, useState } from "react";
import CheckoutSteps from "../components/CheckoutSteps";
import { useCart } from "../context/CartContext";
import Modal from "../components/Modal";
import Card from "../components/Card";
import { useLoading } from "../context/LoadingContext";
import ApiServices from "../api/ApiServices";
import { useUser } from "../context/UserContext";

export default function Cart(){
    const { user, logout } = useUser(); //取得使用者資訊
    const { setLoading } = useLoading(); //取得loading狀態
    const { cartItems, editCart, delCart, clearCart } = useCart(); //取得購物車中內容
    const [currentStep, setCurrentStep] = useState(0); //當前進度

    const [showDelModal, setShowDelModal] = useState(false); //顯示刪除Modal
    const [delModalInfo, setDelModalInfo] = useState({}); //刪除Modal資訊

    const [orderResult, setOrderResult] = useState({}); //訂單結果

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

    //處理刪除餐點
    const handleConfirmDel = ()=>{
        delCart(delModalInfo.id);
    }

    //處理變更數量
    const handleChangeAmount = (id, amount)=>{
        editCart(id, amount);
    }

    //處理送出訂單
    const handleSendOrder = async (_payment)=>{
        try {
            setLoading(true);
            const data = {
                orders: cartItems?.map(item => ({
                    meal_id: item.id,
                    name: item.name,
                    amount: item.amount,
                })),
                payment: _payment,
                total: cartItems?.reduce((total, item) => total + (item.price * item.amount), 0),
                user_id: user,
                location: location,
            };
            const res = await ApiServices.createOrder(data);
            if(res){
                setCurrentStep(2);
                setOrderResult(res);
                //清空購物車
                clearCart();
                logout();
            }
        } catch (error) {
            console.error(`訂單送出失敗:${error}`);
            alert(`訂單送出失敗${error}`);
        } finally {
            setLoading(false);
        }
    }

    return(
        <>
            {cartItems?.length <= 0 && currentStep != 2 ? 
                <p>目前購物車中沒有任何餐點喔!</p>
            :
                <>
                    {/* 上方進度條 */}
                    <CheckoutSteps currentStep={currentStep}/>
                    {/* step1 */}
                    {currentStep == 0 &&
                        <>
                            {/* 主要內容 */}
                            <div className="d-flex flex-column justify-content-between border rounded" style={{height: "calc(100vh - 300px"}}>
                                {/* 購物車內容表格 */}
                                <div className="rounded" style={{height: "100%", overflowY: "scroll"}}>
                                    <table className="table text-center table-striped align-middle">
                                        {/* 標題 */}
                                        <thead className="position-sticky top-0 table-dark" style={{zIndex: "1000"}}>
                                            <tr>
                                                {
                                                    [
                                                        {id:1, title:"餐點名稱"},
                                                        {id:2, title:"類型"},
                                                        {id:3, title:"描述"},
                                                        {id:4, title:"單價"},
                                                        {id:5, title:"數量"},
                                                        {id:6, title:"總金額"}
                                                    ].map((value, index)=>(
                                                        <th key={index}>
                                                            {value.title}
                                                        </th>
                                                    ))
                                                }
                                                <th></th>
                                            </tr>
                                        </thead>
                                        {/* 內容 */}
                                        <tbody>
                                            {
                                                cartItems?.map((item, index)=>(
                                                    <tr key={index}>
                                                        <td>{item.name}</td>
                                                        <td>{item.type}</td>
                                                        <td>{item.description}</td>
                                                        <td>{item.price}</td>
                                                        <td>
                                                            <div className="btn-group">
                                                                <button 
                                                                    className="btn btn-outline-secondary"
                                                                    onClick={()=>{
                                                                        handleChangeAmount(item.id, item.amount-1)
                                                                    }}>
                                                                    <i className="bi bi-dash"></i>
                                                                </button>
                                                                <input 
                                                                    type="number"
                                                                    className="text-center border border-dark"
                                                                    style={{width: "40px"}}
                                                                    value={item.amount}
                                                                    disabled/>
                                                                <button 
                                                                    className="btn btn-outline-secondary"
                                                                    onClick={()=>{
                                                                        handleChangeAmount(item.id, item.amount+1)
                                                                    }}>
                                                                    <i className="bi bi-plus-lg"></i>
                                                                </button>
                                                            </div>
                                                        </td>
                                                        <td>{item.price * item.amount}</td>
                                                        <td>
                                                            <button
                                                                type="button"
                                                                className="btn btn-danger"
                                                                onClick={()=>{
                                                                    setDelModalInfo(item);
                                                                    setShowDelModal(true);
                                                                }}>
                                                                <i className="bi bi-trash3"></i>
                                                            </button>
                                                        </td>
                                                    </tr>
                                                ))
                                            }
                                        </tbody>
                                    </table>
                                </div>
                                {/* Footer */}
                                <div className="alert alert-secondary mb-0">
                                    {/* Footer說明 */}
                                    <div>
                                        <p className="fw-bold fs-5">
                                            共{cartItems?.length}筆餐點，總金額:{
                                                cartItems?.reduce((total, item) => total + (item.price * item.amount), 0)
                                            }
                                        </p>
                                        <p className="fw-bold">請確認購物車中的餐點及數量，確認後點擊下一步選擇付款方式。</p>
                                    </div>
                                    {/* continueBtn */}
                                    <button
                                        type="button"
                                        className="btn btn-primary"
                                        onClick={()=>{
                                            setCurrentStep(1);
                                        }}>
                                        下一步...確認付款方式
                                    </button>
                                </div>
                            </div>
                            {/* 刪除餐點Modal */}
                            <Modal
                                show={showDelModal}
                                title="確認刪除餐點?"
                                onClose={()=>{
                                    setShowDelModal(false);
                                }}
                                onConfirm={()=>{
                                    handleConfirmDel();
                                }}
                                confirmBtnChildren="確認刪除"
                                closeBtnChldren="取消">
                                <p className="alert alert-danger p-2">確認刪除以下餐點?</p>
                                <ul className="list-unstyled alert alert-secondary">
                                    <li>
                                        餐點名稱: {delModalInfo?.name}
                                    </li>
                                    <li>
                                        類型: {delModalInfo.type}
                                    </li>
                                    <li className="fw-bold">
                                        原訂購數量: {delModalInfo.amount}
                                    </li>
                                    <li className="fw-bold">
                                        總價: NT$ {delModalInfo.amount * delModalInfo.price}
                                    </li>
                                </ul>
                            </Modal>
                        </>
                    }
                    {/* step2 */}
                    {currentStep ==1 &&
                        <>
                            {/* 主要內容 */}
                            <div className="d-flex flex-column justify-content-between border rounded" style={{height: "calc(100vh - 300px"}}>
                                {/* 付款方式內容表格 */}
                                <div className=" rounded" style={{height: "100%"}}>
                                    <div className="d-flex justify-content-center align-items-center gap-3 mt-3 h-100">
                                        {
                                            [
                                                {id: 1, title: "現金支付", value: 'cash', img: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQdHEL1eC9InqICNN1xfM1skuVFlct1DeMTJQ&s"},
                                                {id: 2, title: "信用卡支付", value: 'credit', img: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTnTEMZDdPyGwqks3P1uoYhzri7bBZ1mbce8g&s"},
                                                {id: 3, title: "行動支付", value: 'mobile', img: "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQQRkQLm5IUfattRdXimPLqBDSDNWFczrjdLw&s"},
                                            ].map((value, index)=>(
                                                <Card
                                                    key={index}
                                                    title={value.title}
                                                    cardHeight="280px"
                                                    showImg={true}
                                                    imgPath={value.img}
                                                    onClickFunc={()=>{
                                                        handleSendOrder(value.value);
                                                    }}>
                                                    <p className="text-center">選擇{value.title}方式付款</p>
                                                </Card>
                                            ))
                                        }
                                    </div>
                                </div>
                                {/* Footer */}
                                <div className="alert alert-secondary mb-0">
                                    {/* Footer說明 */}
                                    <div>
                                        <p className="fw-bold fs-5">
                                            共{cartItems?.length}筆餐點，總金額:{
                                                cartItems?.reduce((total, item) => total + (item.price * item.amount), 0)
                                            }
                                        </p>
                                    </div>
                                    {/* cancelBtn */}
                                    <button
                                        type="button"
                                        className="btn btn-secondary"
                                        onClick={()=>{
                                            setCurrentStep(0);
                                        }}>
                                        取消，返回上一步
                                    </button>
                                </div>
                            </div>
                        </>
                    } 
                    {/* step3 */}
                    {currentStep == 2 &&
                        <>
                            <div className="d-flex flex-column justify-content-between border rounded" style={{height: "calc(100vh - 400px"}}>
                                <div className="alert alert-secondary mb-0">
                                    <h3 className="text-center">訂單已送出!</h3>
                                    <p className="text-center">訂單編號: {orderResult?.o_id}</p>
                                    <p className="text-center">付款方式: {orderResult?.o_pay}</p>
                                    <p className="text-center">總金額: {orderResult?.o_t}</p>
                                    <ul>
                                        {
                                            orderResult?.o_data.meals.map((item, index)=>(
                                                <li key={index} className="text-center">
                                                    餐點名稱: {item.name}，數量: {item.amount}
                                                </li>
                                            ))
                                        }
                                    </ul>
                                    <p className="text-center">感謝您的訂購!</p>
                                </div>
                            </div>
                            <div className="d-flex justify-content-center mt-3">
                                <a
                                    href="/"
                                    className="btn btn-primary">
                                    返回首頁
                                </a>
                            </div>
                        </>
                    }
                </>
            }
        </>
    )
}