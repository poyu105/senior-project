import { createContext, useContext, useEffect, useState } from "react";
import { useLoading } from "./LoadingContext";

const CartContext = createContext();

export function CartProvider({children}){
    const { setLoading } = useLoading();
    const [cartItems, setCartItems] = useState([]); //購物車物品

    //當cartItem發生變化時，同步更新localstorage
    useEffect(()=>{
        localStorage.setItem("cart", JSON.stringify(cartItems));
    },[cartItems])
    
    //新增至購物車
    const addToCart = (id, name, type, description, price, amount) => {
        try {
            setLoading(true);
            setCartItems((prev) => {
                const isExist = prev?.find(item => item.id === id);
                if (isExist) {
                    return prev.map(item =>
                        item.id === id
                            ? { ...item, amount: item.amount + amount }
                            : item
                    );
                } else {
                    return [...prev, { id, name, type, description, price, amount }];
                }
            });
            alert('成功加入購物車!');
        } catch (error) {
            alert('發生錯誤，無法加入購物車!');
            console.error(`發生錯誤: ${error}`);
        } finally {
            setLoading(false);
        }
    };
    
    //刪除購物車
    const delCart = (id) => {
        try {
            setLoading(true);
            setCartItems((prev) => prev.filter(item => item.id !== id));
            alert('成功刪除!');
        } catch (error) {
            console.error(`發生錯誤: ${error}`);
            alert('發生錯誤，無法刪除!');
        } finally{
            setLoading(false);
        }
    }

    //編輯購物車
    const editCart = (id, amount) => {
        try {
            setLoading(true);
            if(amount > 0){
                setCartItems((prev) =>
                    prev.map(item =>
                        item.id === id ? { ...item, amount } : item
                    )
                );                              
            }else{
                delCart(id);
            }
        } catch (error) {
            console.error(`發生錯誤: ${error}`);
            alert('發生錯誤，變更失敗!');
        } finally{
            setLoading(false);
        }
    }

    return(
        <CartContext.Provider
            value={{
                addToCart,
                cartItems,
                editCart,
                delCart,
            }}>
            {children}
        </CartContext.Provider>
    )
}

export const useCart = ()=>(useContext(CartContext));