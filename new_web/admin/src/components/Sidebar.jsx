import { Link } from "react-router-dom"
import "./Sidebar.css"
import { useAuth } from "../context/AuthContext"
export default function Sidebar({headerTitle}){
    const { username, logout } = useAuth();
    return(
        <>
            <div id="sidebar" className="vh-100 d-flex flex-column justify-content-between text-center text-light bg-dark p-3 border-end border-info border-2">
                <div className="d-flex flex-column justify-content-between align-items-center vh-100">
                    <div className="w-100">
                        <Link
                            to="/"
                            className={`btn text-light fw-bold fs-4 ${headerTitle == "食得其所後台系統"? "active":""}`}
                            >
                        食得其所後台系統
                        </Link>
                        <hr/>
                        <ul className="list-unstyled">
                            <li className="mb-2">
                                <Link
                                    to="/inventory"
                                    className={`btn text-light w-75 ${headerTitle == "庫存管理"? "active":""}`}
                                    >
                                庫存管理
                                </Link>
                            </li>
                            <li className="mb-2">
                                <Link
                                    to="/dailyReport"
                                    className={`btn text-light w-75 ${headerTitle == "每日報表"? "active":""}`}
                                    >
                                    每日報表
                                </Link>
                            </li>
                            <li className="mb-2">
                                <Link
                                    to="/prediction"
                                    className={`btn text-light w-75 ${headerTitle == "銷售預測"? "active":""}`}
                                    >
                                銷售預測
                                </Link>
                            </li>
                        </ul>
                    </div>
                </div>
                <div className="d-flex flex-column">
                    <hr/>
                    <a href="/" className="text-light mb-2">前往前台</a>
                    <div className="d-flex flex-column gap-1">
                         
                        {
                            username ? 
                                (<>
                                    <span>Hello,{username}</span>
                                    <div className="d-flex justify-content-center gap-3">
                                        <Link className="btn btn-outline-light rounded-pill" to="/register"><i className="bi bi-person-add"></i></Link>
                                        <button className="btn btn-outline-warning rounded-pill" onClick={()=>logout()}>
                                            <i className="bi bi-box-arrow-right"></i>
                                        </button>
                                    </div>
                                </>) : 
                                (<Link to="/login">登入</Link>)
                        }
                    </div>
                </div>
            </div>
        </>
    )
}