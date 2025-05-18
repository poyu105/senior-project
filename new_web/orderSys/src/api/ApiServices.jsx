const API_BASE_URL = "https://localhost:7220/api";

const fetchData = async (url, method = 'GET', data = null) => {
    const headers = {
        'Content-Type' : 'application/json',
    };
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
    getMeals: () => fetchData('/Customer/getMeals'), //取得餐點列表
    createOrder: (data) => fetchData('/Customer/createOrder', 'POST', data), //建立訂單

    login: (photo) => fetchData('/Auth/customer-login', 'POST', photo), //登入
}
export default ApiServices;