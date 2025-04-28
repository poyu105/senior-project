import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useCart } from "../context/CartContext";
import "./Navbar.css";

export default function Navbar(){
    const { cartItems } = useCart();
    const [cartCount, setCartCount] = useState(0); //購物車中物品數量

    //監控購物車變化並設定購物車中物品數量
    useEffect(()=>{
        setCartCount(cartItems?.length);
    },[cartItems])

    return(
        <>
            <nav className="navbar shadow nb">
                <a href="/" className="navbar-brand text-black fs-3 mx-3">無人泡麵</a>
                <ul className="flex-row navbar-nav align-items-center gap-2 mx-3">
                    {/* 菜單 */}
                    <li className="nav-item px-2">
                        <Link to="/" className="nav-link text-black">菜單</Link>
                    </li>
                    {/* 購物車 */}
                    <li className="nav-item px-2">
                       <Link to="/cart" className="text-decoration-none text-black position-relative">
                            購物車
                            <i className="bi bi-cart"></i>
                            {cartCount >0 && (
                                <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger">
                                    {cartCount}
                                </span>
                            )}
                        </Link>
                    </li>
                </ul>
            </nav>
        </>
    )
}