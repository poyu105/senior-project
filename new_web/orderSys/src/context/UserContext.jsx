import { createContext, useContext, useState } from "react";
import { useLoading } from "./LoadingContext";
import ApiServices from "../api/ApiServices";

const UserContext = createContext();

export function UserProvider({children}){
    const { setLoading } = useLoading();
    const [user, setUser] = useState(null); //使用者資訊

    //執行登入操作
    const login = async (photo)=>{
        try {
            setUser(null);
            setLoading(true);
            const res = await ApiServices.login(photo);
            if(res.success){
                alert("登入成功!");
                setUser(res.token);
                //window.location.reload();
                sessionStorage.setItem("token", res.token);
            }else{
                alert("登入失敗: " + res.message);
                setUser(null);
            }
        } catch (error) {
            console.error(`發生錯誤: ${error}`);
        } finally {
            setLoading(false);
        }
    }

    //註冊
    const register = async (photo, info) => {
        try {
            setLoading(true);
            var registerData = { 
                photos: photo,
                username: info.name,
                phone_number: info.phone, 
            };
            const res = await ApiServices.register(registerData);
            if(res.success){
                alert(res.message);
            }else{
                alert("註冊失敗: " + res.message);
            }
        } catch (error) {
            console.error(`發生錯誤: ${error}`);
        } finally {
            setLoading(false);
            window.location.reload();
        }
    }

    //登出
    const logout = () => {
        setUser(null);
        sessionStorage.removeItem("token");
        //window.location.reload();
    }
    
    return(
        <UserContext.Provider
            value={{
                login,
                register,
                user,
                logout,
            }}>
            {children}
        </UserContext.Provider>
    )
}

export const useUser = ()=>(useContext(UserContext));