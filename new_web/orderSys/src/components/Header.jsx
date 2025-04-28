import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom"

export default function Header(){
    const location = useLocation(); //取得當前位置(path)
    const [title, setTitle] = useState(''); //標題

    //渲染標題
    useEffect(()=>{
        const titlemap = {
            "/":"菜單",
            "/cart":"購物車",
            "/register":"註冊"
        }
        setTitle(titlemap[location.pathname]);
    },[location])
    return(
        <>
            <div className="my-3 border-bottom">
                <h1>{title}</h1>
            </div>
        </>
    )
}