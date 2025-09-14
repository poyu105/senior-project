const API_BASE_URL = "https://localhost:7220/api";

const fetchData = async (url, method = 'GET', data = null) => {
    const token = sessionStorage.getItem("token"); //取得token
    
    const headers = {
        'Content-Type' : 'application/json',
    };

    if(token){
        headers["Authorization"] = `Bearer ${token}`;
    }

    const options = {
        headers,
        method,
    };
    if(data){
        options.body = JSON.stringify(data);
    }

    try {
        const response = await fetch(`${API_BASE_URL}${url}`, options);
        if(!response.ok){
            const errorData = await response.json();
            throw new Error(errorData.message || "API發生錯誤!");
        }
        return await response.json();
    } catch (error) {
        console.error("發生錯誤: "+error.message);
        alert(`發生錯誤:${error.message}`);
    }
}

const ApiServices = {
    getMeals: () => fetchData('/Customer/getMeals', 'GET'), //取得餐點列表
    createOrder: (data) => fetchData('/Customer/createOrder', 'POST', data), //建立訂單

    login: (photo) => fetchData('/Auth/customer-login', 'POST', photo), //登入
    register: (info) => fetchData('/Auth/customer-register', 'POST', info), //註冊
}
export default ApiServices;