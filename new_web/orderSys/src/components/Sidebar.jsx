import "./Sidebar.css";
export default function Sidebar({ selectType, setSelectType }){
    return(
        <>
            <ul className="sidebar-list text-center rounded">
                <li 
                    className={`py-2 my-1 w-75 rounded ${selectType == "recommend" ? "active" : ""}`}
                    onClick={()=>{
                        setSelectType("recommend");
                    }}>
                    推薦餐點
                </li>
                <li 
                    className={`py-2 my-1 w-75 rounded ${selectType == "seafood" ? "active" : ""}`}
                    onClick={()=>{
                        setSelectType("seafood");
                    }}>
                    海鮮泡麵
                </li>
                <li 
                    className={`py-2 my-1 w-75 rounded ${selectType == "spicy" ? "active" : ""}`}
                    onClick={()=>{
                        setSelectType("spicy");
                    }}>
                    辛辣泡麵
                </li>
                <li 
                    className={`py-2 my-1 w-75 rounded ${selectType == "vegetarian" ? "active" : ""}`}
                    onClick={()=>{
                        setSelectType("vegetarian");
                    }}>
                    素食泡麵
                </li>
                <li 
                    className={`py-2 my-1 w-75 rounded ${selectType == "all" ? "active" : ""}`}
                    onClick={()=>{
                        setSelectType("all");
                    }}>
                    顯示全部
                </li>
            </ul>
        </>
    )
}