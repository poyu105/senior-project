import { Route, Routes } from 'react-router-dom';
import Home from './pages/Home';
import Cart from './pages/Cart';
import Register from './pages/Register';
export default function AppRoutes(){
    return(
        <>
            <Routes>
                <Route path='/' element={<Home/>}/>
                <Route path='/cart' element={<Cart/>}/>
                <Route path='/register' element={<Register/>}/>
            </Routes>
        </>
    )
}