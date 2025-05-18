import { useState } from "react";
import ApiServices from "../api/ApiServices";
import { useAuth } from "../context/AuthContext"
import { useLoading } from "../context/LoadingContext";
import { useNavigate } from "react-router-dom";

export default function Login(){
    const navigate = useNavigate();
    const { setLoading } = useLoading();
    const { login, setUsername } = useAuth();
    const [account, setAccount] = useState("");
    const [password, setPassword] = useState("");

    const handleSubmit = async (e)=>{
        //執行表單驗證
        e.preventDefault();
        var form = document.getElementById('login-form');
        if(!form.checkValidity()){
            e.stopPropagation();
            form.classList.add("was-validated");
            return;
        }

        try {
            setLoading(true);
            var data = {
                account: account,
                password: password,
            };
            const res = await ApiServices.login(data, login);
            if(res){
                login(res.token, res.user);
                alert(res.message);
                navigate('/');
            }
        } catch (error) {
            console.error(`發生錯誤: ${error}`);
        }
        setLoading(false);
    }
    return(
        <>
            <div className="container h-75 d-flex justify-content-center align-items-center">
                <form id="login-form" onSubmit={handleSubmit} className="col-6 border p-4 rounded needs-validation" noValidate>
                    <h2>登入</h2>
                    <div className="mb-3">
                        <label className="form-label">使用者帳號</label>
                        <input
                            type="text"
                            placeholder="請輸入使用者帳號"
                            className="form-control"
                            value={account}
                            onChange={(e)=>setAccount(e.target.value)}
                            required/>
                        <span className="invalid-feedback">請輸入帳號!</span>
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
                            <span className="invalid-feedback">請輸入密碼!</span>
                    </div>
                    <div>
                        <button
                            type="submit"
                            className="btn btn-success w-100">
                            登入
                        </button>
                    </div>
                </form>
            </div>
        </>
    )
}