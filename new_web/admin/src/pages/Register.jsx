import { useState } from "react";
import { useLoading } from "../context/LoadingContext"
import ApiServices from "../api/ApiServices";
import { useNavigate } from "react-router-dom";

export default function Register(){
    const navigate = useNavigate();
    const { setLoading } = useLoading();
    const [username, setUsername] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const handleSubmit = async (e)=>{
        //執行表單驗證
        e.preventDefault();
        var form = document.getElementById('register-form');
        if(!form.checkValidity() || password != confirmPassword){
            e.stopPropagation();
            form.classList.add("was-validated");
            return;
        }

        try {
            setLoading(true);
            var data = {
                username: username,
                password: password,
            };
            
            const res = await ApiServices.register(data);
            if(res){
                alert(res.message);
                navigate('/login');
                form.submit();
            }
        } catch (error) {
            console.error(`發生錯誤: ${error}`);
        }
        setLoading(false);
    }

    return(
        <>
            <div className="container h-75 d-flex justify-content-center align-items-center">
                <form id="register-form" onSubmit={handleSubmit} className="col-6 border p-4 rounded needs-validation" noValidate>
                    <h2>註冊用戶</h2>
                    <div className="mb-3">
                        <label className="form-label">使用者名稱</label>
                        <input
                            type="text"
                            placeholder="請輸入使用者名稱"
                            className="form-control"
                            value={username}
                            onChange={(e)=>setUsername(e.target.value)}
                            required/>
                        <span className="invalid-feedback">請輸入使用者名稱</span>
                    </div>
                    <div className="mb-3">
                        <label className="form-label">密碼</label>
                        <input
                            type="password"
                            placeholder="請輸入密碼"
                            className="form-control"
                            value={password}
                            onChange={(e)=>setPassword(e.target.value)}
                            required/>
                        <span className="invalid-feedback">請輸入密碼</span>
                    </div>
                    <div className="mb-3">
                        <label className="form-label">確認密碼</label>
                        <input
                            type="password"
                            placeholder="請再次輸入密碼"
                            className="form-control"
                            value={confirmPassword}
                            onChange={(e)=>setConfirmPassword(e.target.value)}
                            required/>
                        <span className="invalid-feedback">請再次輸入密碼</span>
                        {confirmPassword != password &&
                            (<span className="w-confirm text-danger" style={{fontSize: "14px"}}>輸入的密碼不同</span>)}
                    </div>
                    <div>
                        <button 
                            type="submit" 
                            className="btn btn-success w-100">
                            註冊
                        </button>
                    </div>
                </form>
            </div>
        </>
    )
}