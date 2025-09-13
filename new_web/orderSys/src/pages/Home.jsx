import { useEffect, useRef, useState } from "react";
import Sidebar from "../components/Sidebar";
import { useLoading } from "../context/LoadingContext";
import ApiServices from "../api/ApiServices";
import "./Home.css";
import Card from "../components/Card";
import Modal from "../components/Modal";
import { useCart } from "../context/CartContext";
import { useUser } from "../context/UserContext";

export default function Home(){
    const { setLoading } = useLoading();
    const { addToCart } = useCart(); //購物車Context
    const { login, register } = useUser(); //用戶Context
    
    const [meals, setMeals] = useState([]); //餐點列表

    const [selectType, setSelectType] = useState("all"); //選取的分類

    const [showAddCartModal, setShowAddCartModal] = useState(false); //顯示加入購物車Modal
    const [mealInfo, setMealInfo] = useState(null); //顯示於購物車Modal的資訊
    const [amount, setAmount] = useState(1); //加入購物車數量

    const [showLoginModal, setShowLoginModal] = useState(false); //顯示登入Modal
    const photoRef = useRef([]); //圖片參考
    const [photo, setPhoto] = useState([]); // 用來存儲拍攝的照片
    const [captureIntervalCount, setCaptureIntervalCount] = useState(3); //計時器
    let cleanup = null;  // 用來儲存清理相機流的函數

    const [showRegisterModal, setShowRegisterModal] = useState(false); //顯示註冊Modal
    const [registerInfo, setRegisterInfo] = useState({name: "", phone: ""}); //註冊資訊
    const [showRegisterCamera, setShowRegisterCamera] = useState(false); //顯示註冊相機

    //取得餐點資訊
    useEffect(()=>{
        const fetchData = async ()=>{
            setLoading(true);
            try {
                const res = await ApiServices.getMeals();
                setMeals(res);
            } catch (error) {
                console.error(`發生錯誤: ${error}`);
            }
            setLoading(false);
        }
        fetchData();
    },[])

    //監控加入購物車數量變化
    useEffect(()=>{
        if (amount !== 1 && (amount <= 0 || amount > 100)) {
            setAmount(1);
            alert('數量最少為1，最多為100!');
        }
    },[amount])

    //處理新增至購物車
    const handleAddToCart = () => {
        addToCart(mealInfo.id, mealInfo.name, mealInfo.type, mealInfo.description, mealInfo.price, amount); //加入購物車
        setAmount(1); //重置數量
        setShowAddCartModal(false); //關閉加入購物車Modal
    }

    // 啟動相機
    const startCamera = async () => {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ video: true });
            const video = document.getElementById("video");
            video.srcObject = stream;

            // 每秒捕捉一張照片並更新state
            const captureInterval = setInterval(() => {
                const canvas = document.createElement("canvas");
                const context = canvas.getContext("2d");
                canvas.width = video.videoWidth;
                canvas.height = video.videoHeight;
                context.drawImage(video, 0, 0, canvas.width, canvas.height);
                const imgData = canvas.toDataURL("image/png");
                setPhoto(prev => [...prev, imgData]); // 儲存拍攝的圖片
                photoRef.current.push(imgData); // 即時儲存

                setCaptureIntervalCount(prev => {
                    if (prev > 0) {
                        return prev - 1;
                    } else {
                        endCamera(); //拍完三張照片後停止
                        //計時結束後進行登入
                        setTimeout(() => {
                            showLoginModal ? login(photoRef.current) : register(photoRef.current, registerInfo); //執行登入或註冊
                        }, 0);
                        setShowLoginModal(false); //關閉登入Modal
                    }
                });
            }, 300); // 每0.3秒更新一次

            // 儲存清理函數
            cleanup = () => {
                clearInterval(captureInterval);  // 停止捕捉圖片
                stream.getTracks().forEach(track => track.stop());  // 停止相機流
            };
        } catch (error) {
            console.error("無法啟動相機: ", error);
        }
    };

    // 手動停止相機
    const endCamera = () => {
        if (cleanup) {
            cleanup();  // 呼叫清理函數停止相機流
        }
    };

    // 當 showLoginModal, showRegisterModal 改變時執行
    useEffect(() => {
        if (showLoginModal || showRegisterModal) {
            setPhoto([]); //重置photo內容
            photoRef.current = []; //清空照片暫存
            startCamera();  // 啟動相機
            setCaptureIntervalCount(3); //設定計時器為3秒
        } else {
            endCamera();  // 在Modal關閉時停止相機
        }

        return () => {
            endCamera();  // 確保組件卸載時清理
        };
    }, [showLoginModal, showRegisterCamera]);

    //進行註冊資料驗證
    const handleRegister = () => {
        var form = document.getElementById("registerForm");
        if(!form.checkValidity()){
            form.classList.add("was-validated");
            return;
        }
        setShowRegisterCamera(true);
    }

    return(
        <>
            {/* 主要內容區 */}
            <div className="d-flex" style={{height: "calc(100vh - 175px)"}}>
                {/* Sidebar */}
                <div className="col-2 text-center">
                    <Sidebar selectType={selectType} setSelectType={setSelectType}/>
                    {/* 登入Btn */}
                    <button
                        type="button"
                        className="btn btn-warning w-100 mb-2"
                        onClick={()=>{
                            setShowLoginModal(true);
                        }}>
                        快速登入
                    </button>
                    {/* 註冊Btn */}
                    <button
                        type="button"
                        className="btn btn-outline-secondary w-100 mt-2"
                        onClick={()=>{
                            setShowRegisterModal(true);
                        }}>
                        註冊
                    </button>
                </div>
                {/* 餐點Card */}
                <div className="mx-auto p-0 col-9">
                    <div className="card-grid border border-2 border-warning rounded-4">
                        {meals.filter(meal => selectType == "all" ? meal : meal.type == selectType).map((meal, index) => (
                            <Card
                                key={index}
                                showImg={true}
                                imgPath={`https://localhost:7220/${meal.img_path}`}
                                title={meal.name}
                                onClickFunc={()=>{
                                        setShowAddCartModal(true)
                                        setMealInfo(meal);
                                    }}>
                                <ul className="list-unstyled text-secondary" style={{fontSize: "14px"}}>
                                    <li>餐點類型: {meal.type}</li>
                                    <li>
                                        餐點介紹:
                                        <p>{meal.description}</p>
                                    </li>
                                </ul>
                                <div className="d-flex flex-row justify-content-between align-items-center gap-2">
                                    <p className="card-text fs-5 m-0">NT$ {meal.price}</p>
                                    <button className="btn bg-light border">
                                        <i className="bi bi-cart-plus"></i>
                                    </button>
                                </div>
                            </Card>
                        ))}
                    </div>
                </div>
            </div>
            
            {/* 加入購物車Card */}
            <Modal
                show={showAddCartModal}
                title="加入購物車"
                onClose={()=>{
                    setShowAddCartModal(false);
                }}
                onConfirm={()=>{
                    handleAddToCart();
                }}
                confirmBtnChildren={"加入購物車"}
                closeBtnChldren={"取消"}>
                <div className="mx-auto border border-3 rounded" style={{width: "80%"}}>
                    <img
                        style={{width: "100%", height: "250px", objectFit: "cover"}}
                        src={`https://localhost:7220/${mealInfo?.img_path}`}/>
                    <div className="p-2">
                        <ul className="list-unstyled m-0">
                            <li className="fs-3">{mealInfo?.name}</li>
                            <li>餐點類型: {mealInfo?.type}</li>
                            <li>
                                餐點描述:
                                <p className="m-0">
                                    {mealInfo?.description}
                                </p>
                            </li>
                            <li>單價NT${mealInfo?.price}</li>
                        </ul>
                        <div className="d-flex align-items-center justify-content-between">
                            <div>
                                <span className="me-3">請選擇數量</span>
                                {/* 減少數量Btn */}
                                <button
                                    type="button"
                                    className="btn btn-outline-secondary p-1 rounded-3"
                                    onClick={()=>setAmount(prev=>prev-1)}>
                                    <i className="bi bi-dash"></i>
                                </button>
                                {/* 數量 */}
                                <input
                                    type="number"
                                    className="text-center mx-2 rounded border border-secondary"
                                    style={{width: "50px"}}
                                    value={amount}
                                    onChange={(e)=>setAmount(e.target.value)}
                                    required/>
                                {/* 增加數量Btn */}
                                <button
                                    type="button"
                                    className="btn btn-outline-secondary p-1 rounded-3"
                                    onClick={()=>setAmount(prev=>prev+1)}>
                                    <i className="bi bi-plus"></i>
                                </button>
                            </div>
                            <p className="m-0">價格: NT$ <span className="fs-2">{amount*mealInfo?.price}</span></p>
                        </div>
                    </div>
                </div>
            </Modal>

            {/* 登入Modal */}
            <Modal
                show={showLoginModal}
                title="快速登入"
                showConfirmBtn={false}
                onClose={()=>{
                    setShowLoginModal(false);
                    endCamera();
                }}
                closeBtnChldren={"取消"}>
                {/* 顯示相機視窗 */}
                {captureIntervalCount >= 0 && (
                        // 拍照畫面
                        <>
                            {/* 倒數計時 */}
                            <p className="fs-5 alert alert-danger">請將正臉對準相機!&emsp; {/* <strong className="fw-bold">請稍後:{captureIntervalCount}</strong> */}</p>
                            {/* 相機視窗 */}
                            <div className="text-center">
                                <video
                                    id="video"
                                    autoPlay
                                    playsInline
                                    style={{ width: "100%", maxHeight: "300px", objectFit: "cover" }}
                                ></video>
                                <canvas
                                    id="canvas"
                                    style={{ display: "none" }}
                                ></canvas>
                            </div>
                        </>
                    )
                }
            </Modal>

            {/* 註冊Modal */}
            <Modal
                show={showRegisterModal}
                title={"註冊會員"}
                onClose={()=>{
                    setShowRegisterModal(false);
                    endCamera();
                }}
                closeBtnChldren={"取消"}
                confirmBtnChildren={"儲存"}>
                {/* 填寫表格 */}
                {
                    !showRegisterCamera && (
                        <form id="registerForm" className="needs-validation" noValidate>
                            <div className="mb-3">
                                <label className="form-label">名稱</label>
                                <input 
                                    type="text" 
                                    className="form-control" 
                                    placeholder="請填寫名稱" 
                                    value={registerInfo?.name}
                                    onChange={(e)=>{
                                        setRegisterInfo({...registerInfo, name: e.target.value});
                                    }}
                                    required/>
                                <span className="invalid-feedback">請輸入名稱</span>
                            </div>
                            <div className="mb-3">
                                <label className="form-label">電話</label>
                                <input 
                                    type="tel" 
                                    className="form-control" 
                                    placeholder="請填寫電話" 
                                    value={registerInfo?.phone}
                                    onChange={(e)=>{
                                        setRegisterInfo({...registerInfo, phone: e.target.value});
                                    }}
                                    required/>
                                <span className="invalid-feedback">請輸入電話</span>
                            </div>
                            <button
                                type="button"
                                className="btn btn-outline-secondary w-100 mt-2"
                                onClick={()=>{
                                    handleRegister();
                                }}>
                                進行人臉辨識註冊
                            </button>
                        </form>
                    )
                }
                
                {/* 顯示相機視窗 */}
                {(captureIntervalCount >= 0 && showRegisterCamera) && (
                        // 拍照畫面
                        <>
                            {/* 倒數計時 */}
                            <p className="fs-5 alert alert-danger">請將正臉對準相機!&emsp; {/* <strong className="fw-bold">請稍後:{captureIntervalCount}</strong> */}</p>
                            {/* 相機視窗 */}
                            <div className="text-center">
                                <video
                                    id="video"
                                    autoPlay
                                    playsInline
                                    style={{ width: "100%", maxHeight: "300px", objectFit: "cover" }}
                                ></video>
                                <canvas
                                    id="canvas"
                                    style={{ display: "none" }}
                                ></canvas>
                            </div>
                        </>
                    )
                }
            </Modal>
        </>
    )
}