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
    
    return(
        <UserContext.Provider
            value={{
                login,
                user,
            }}>
            {children}
        </UserContext.Provider>
    )
}

export const useUser = ()=>(useContext(UserContext));