import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom"

export default function Header({selectType}){
    // const location = useLocation(); //取得當前位置(path)
    const [title, setTitle] = useState(''); //標題

    //渲染標題
    useEffect(()=>{
        const titlemap = {
            "recommend":"推薦餐點",
            "seafood":"海鮮泡麵",
            "spicy":"辛辣泡麵",
            "vegetarian":"素食泡麵",
            "all":"所有餐點",
        }
        setTitle(titlemap[selectType]);
    },[selectType])
    return(
        <>
            <div className="my-3 mx-4">
                <h1 className="fw-bold fs-2" style={{color: "#1f2937"}}>{title}</h1>
            </div>
        </>
    )
}