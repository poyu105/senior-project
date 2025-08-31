import { createContext, useContext, useState } from "react";
import { useLoading } from "./LoadingContext";
import ApiServices from "../api/ApiServices";

const UserContext = createContext();

export function UserProvider({children}){
    const { setLoading } = useLoading();
    const [user, setUser] = useState({}); //使用者資訊

    //執行登入操作
    const login = async (photo)=>{
        try {
            setUser(null);
            setLoading(true);
            const res = await ApiServices.login(photo);
            if(res.success){
                window.location.reload();
                alert("登入成功!");
                setUser(res.user);
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
    
    return(
        <UserContext.Provider
            value={{
                login,
                register,
                user,
            }}>
            {children}
        </UserContext.Provider>
    )
}

export const useUser = ()=>(useContext(UserContext));