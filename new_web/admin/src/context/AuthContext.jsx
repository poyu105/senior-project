import { createContext, useContext, useState } from "react";

const AuthContext = createContext();

export function AuthProvider({children}){
    const [token, setToken] = useState(sessionStorage.getItem("token") || null);
    const [username, setUsername] = useState(sessionStorage.getItem("user") || null);

    const login = (jwtToken, username) => {
        sessionStorage.setItem("token", jwtToken);
        sessionStorage.setItem("user", username);
        setToken(jwtToken);
        setUsername(username);
    }

    const logout = () => {
        sessionStorage.removeItem("token");
        sessionStorage.removeItem("user");
        setToken(null);
        setUsername("");
    }

    const isAuthenticated = !!token;

    return(
        <AuthContext.Provider value={{token, username, setUsername, login, logout, isAuthenticated}}>
            {children}
        </AuthContext.Provider>
    )
}

export function useAuth(){
    return useContext(AuthContext);
}