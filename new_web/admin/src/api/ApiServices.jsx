const API_BASE_URL = "https://localhost:7220/api";

const fetchData = async (url, method = "GET", data = null) => {
    const token = sessionStorage.getItem("token"); //取得token

    const headers = {
        "Content-Type": "application/json",
    };

    if(token){
        headers["Authorization"] = `Bearer ${token}`;
    }

    const options = {
        method,
        headers,
    }

    if(data){
        options.body = JSON.stringify(data);
    }

    try {
        const response = await fetch(`${API_BASE_URL}${url}`, options);
        if(response.status === 401){
            alert('登入過期，請重新登入!');
            sessionStorage.clear();
            window.location.reload();
            return;
        }
        if(!response.ok){
            const errorData = await response.json();
            throw new Error(errorData.message || "API發生錯誤");
        }
        return await response.json();
    } catch (error) {
        console.error("發生錯誤: "+error.message);
        alert(error.message);
    }
};

const ApiServices = {
    login: (data) => fetchData('/Auth/admin-login', 'POST', data),    //登入
    register: (data) => fetchData('/Auth/admin-register', 'POST', data),  //註冊

    getSales: () => fetchData('/Admin/getSales', 'GET'),    //取得銷售資料

    getInventory: () => fetchData('/Admin/getInventory'),  //取得庫存資料
    addInventory: (data) => fetchData('/Admin/addInventory', 'POST', data),  //新增庫存資料
    deleteInventory: (id) => fetchData(`/Admin/deleteInventory/${id}`, 'DELETE'),  //刪除庫存資料
    editInventory: (data) => fetchData('/Admin/editInventory', 'PUT', data),  //編輯庫存資料

    getPrediction: (data) => fetchData('/Admin/getPrediction', 'POST', data),  //取得預測銷售資料
}

export default ApiServices;