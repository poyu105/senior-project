import { createContext, useContext, useState } from "react";
import { useLoading } from "./LoadingContext";

const UserContext = createContext();

export function UserProvider({children}){
    const { setLoading } = useLoading();
    const [user, setUser] = useState({}); //使用者資訊

    //執行登入操作
    const login = async (photo)=>{
        try {
            setLoading(true);
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
            }}>
            {children}
        </UserContext.Provider>
    )
}

export const useUser = ()=>(useContext(UserContext));