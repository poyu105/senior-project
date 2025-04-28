import { createContext, useContext, useState } from "react";
import "./LoadingContext.css"

const LoadingContext = createContext();
export function LoadingProvider({children}){
    const [loading, setLoading] = useState(false);

    return(
        <LoadingContext.Provider value={{loading, setLoading}}>
            {children}
            {loading && (
                <div className="position-fixed top-0 start-0 w-100 h-100 bg-dark bg-opacity-50 d-flex justify-content-center align-items-center flex-column" style={{zIndex: "2000"}}>
                    <div className="spinner-border text-light" role="status"></div>
                    <span className="text-white">載入中<span className="dots"></span>請勿離開視窗</span>
                </div>
            )}
        </LoadingContext.Provider>
    )
}
export function useLoading(){
    return useContext(LoadingContext);
}