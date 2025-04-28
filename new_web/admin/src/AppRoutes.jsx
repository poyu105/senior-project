import { Router, Route, Routes, Navigate } from 'react-router-dom';
import Home from './pages/Home';
import Sidbar from './components/Sidebar';
import { useState } from 'react';
import Header from './components/Header';
import { useAuth } from './context/AuthContext';
import Login from './pages/Login';
import Register from './pages/Register';
import Inventory from './pages/Inventory';
import DailyReport from './pages/DailyReport';
import Prediction from './pages/Prediction';

function PrivateRoute({children}){
    const { isAuthenticated } = useAuth();
    return isAuthenticated ? children : <Navigate to="/login"/>;
}

export default function AppRoutes(){
    //頁標題
    const [headerTitle, setHeaderTitle] = useState('');
    //搜尋欄
    const [search, setSearch] = useState('');

    return(
        <>
                <div className="container-fluid row p-0">
                    <div className="col-2 p-0">
                        <Sidbar headerTitle={headerTitle}/>
                    </div>
                    <div className="col-10 p-4 pb-2">
                        <Header headerTitle={headerTitle} setHeaderTitle={setHeaderTitle} search={search} setSearch={setSearch} />
                        <Routes>
                            <Route path='/login' element={<Login/>}/>
                            <Route path='/register' element={<Register/>}/>
                            <Route path='/' element={<PrivateRoute><Home/></PrivateRoute>}/>
                            <Route path='/inventory' element={<PrivateRoute><Inventory/></PrivateRoute>}/>
                            <Route path='/dailyReport' element={<PrivateRoute><DailyReport/></PrivateRoute>}/>
                            <Route path='/prediction' element={<PrivateRoute><Prediction/></PrivateRoute>}/>
                        </Routes>
                    </div>
                </div>
        </>
    )
}