const API_BASE_URL = import.meta.env.VITE_BASE_URL+"api";

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

        let data;
        try {
            data = await response.json();
        } catch {
            data = null; // 如果不是 JSON 就設為 null
        }

        if (!response.ok) {
            const errorMessage = data && data.message ? data.message : "API發生錯誤";
            throw new Error(errorMessage);
        }

        return await data;
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

    getReportData: (date) => fetchData(`/Admin/getReportData?date=${date}`), //取得報表資料
    saveReports: (data) => fetchData(`/Admin/saveReport`, 'POST', data), //儲存報表資料
}

export default ApiServices;