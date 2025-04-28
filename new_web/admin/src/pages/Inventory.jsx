import { useEffect, useState } from "react";
import { useLoading } from "../context/LoadingContext"
import ApiServices from "../api/ApiServices";
import Modal from "../components/Modal";

export default function Inventory(){
    const { setLoading } = useLoading();
    const [inventory, setInventory] = useState([]); //庫存資料

    const [view, setView] = useState("table"); //表格或圖表的顯示方式

    const [showAddInventoryModal, setShowAddInventoryModal] = useState(false); //顯示新增庫存的Modal
    const [addNewInventory, setAddNewInventory] = useState({}); //新增庫存的資料

    const [showEditInventoryModal, setShowEditInvemtoryModal] = useState(false); //顯示編輯庫存Modal
    const [editInventory, setEditInventory] = useState({}); //編輯庫存資料

    // 取得庫存資料
    useEffect(()=>{
        try {
            setLoading(true);
            const fetchInventory = async ()=>{
                const res = await ApiServices.getInventory();
                if(res){
                    setInventory(res);
                }
            }
            fetchInventory();
        } catch (error) {
            console.error("發生錯誤: "+error.message);
        } finally{
            setLoading(false);
        }
    },[])

    // 新增庫存資料
    const handleAddInventory = async ()=>{
        var form = document.getElementById("addForm");
        if(!form.checkValidity()){
            form.classList.add("was-validated");
            return;
        }
        try {
            setLoading(true);
            const res = await ApiServices.addInventory(addNewInventory);
            if(res.success){
                alert(res.message);
                window.location.reload();
            }
        } catch (error) {
            console.error("發生錯誤: "+error.message);
        } finally{
            setLoading(false);
            setShowAddInventoryModal(false); //關閉Modal
            setAddNewInventory({}); //清空新增庫存的資料
        }
    }

    // 刪除庫存資料
    const handleDeleteInventory = async (id)=>{
        try {
            setLoading(true);
            const res = await ApiServices.deleteInventory(id);
            if(res){
                alert(res.message);
                setInventory(inventory.filter(item=>item.id!==id)); //刪除庫存資料
            }
        } catch (error) {
            console.error("發生錯誤: "+error.message);
        } finally{
            setLoading(false);
        }
        
    }

    //編輯庫存資料
    const handleEditInventory = async ()=>{
        var form = document.getElementById('editForm');
        if(!form.checkValidity()){
            form.classList.add('was-validated');
            return;
        }
        try {
            setLoading(true);
            const data = {
                id: editInventory?.id,
                name: editInventory?.name,
                type: editInventory?.type,
                description: editInventory?.description,
                cost: editInventory?.cost,
                price: editInventory?.price,
                img_path: editInventory?.new_img_path ? editInventory?.new_img_path : editInventory?.img_path,
                quantity: editInventory?.quantity,
            }
            const res = await ApiServices.editInventory(data);
            if(res.success){
                alert(res.message);
                window.location.reload();
            }
        } catch (error) {
            console.error(`發生錯誤: ${error.message}`);
            alert('編輯過程中發生錯誤!');
        } finally {
            setLoading(false);
            setShowEditInvemtoryModal(false);
            setEditInventory({});
        }
    }

    return(
        <>
            <div>
                {/* 庫存內容上方選項 */}
                <div className="d-flex justify-content-between align-items-center">
                    <h5 className="m-0">共{inventory.length}筆資料</h5>
                    {/* 切換顯示方式 */}
                    <div className="btn-group mb-1">
                        {/* 以表格方式呈現 */}
                        <button
                            type="button"
                            title="切換成表格"
                            className={`btn ${view==="table" ? "btn-primary" : "btn-outline-primary"}`}
                            onClick={()=>setView("table")}>
                            <i className="bi bi-table"></i>
                        </button>
                        {/* 以圖表方式呈現 */}
                        <button
                            type="button"
                            title="切換成圖表"
                            className={`btn ${view==="chart" ? "btn-primary" : "btn-outline-primary"}`}
                            onClick={()=>setView("chart")}>
                            <i className="bi bi-bar-chart-line-fill"></i>
                        </button>
                    </div>
                </div>
                {/* 以表格方式顯示 */}
                {view==="table" && (
                    <div style={{height: "calc(100vh - 210px)", overflowY: "scroll"}}>
                        {/* 庫存表格 */}
                        <table className="table table-striped table-hover table-bordered text-center align-middle">
                            {/* 庫存表格標題 */}
                            <thead className="position-sticky top-0 table-dark">
                                <tr>
                                    <th className="col">圖片</th>
                                    <th className="col">名稱</th>
                                    <th className="col">類型</th>
                                    <th className="col-4">描述</th>
                                    <th className="col">成本</th>
                                    <th className="col">售價</th>
                                    <th className="col">庫存</th>
                                    <th className="col-2">編輯/刪除</th>
                                </tr>
                            </thead>
                            {/* 庫存表格主要內容 */}
                            <tbody className="table-group-divider">
                                {inventory.map((item, index)=>(
                                    <tr key={index}>
                                        <td>
                                            <img src={`https://localhost:7220/${item.img_path}`} alt={item.name} className="img-fluid" style={{width: "100px", height: "100px"}}/>
                                        </td>
                                        <td>{item.name}</td>
                                        <td>
                                            {
                                                [
                                                    {id: "seafood", title: "海鮮泡麵"},
                                                    {id: "spicy", title: "辛辣泡麵"},
                                                    {id: "vegetarian", title: "素食泡麵"},
                                                ].find((option => option.id == item.type))?.title
                                            }
                                        </td>
                                        <td>{item.description}</td>
                                        <td>{item.cost}</td>
                                        <td>{item.price}</td>
                                        <td>{item.quantity}</td>
                                        <td>
                                            {/* 編輯按鈕 */}
                                            <button
                                                type="button"
                                                className="btn btn-warning"
                                                onClick={()=>{
                                                    setEditInventory(item);
                                                    setShowEditInvemtoryModal(true);
                                                }}>
                                                <i className="bi bi-pencil"></i>
                                            </button>
                                            {/* 刪除按鈕 */}
                                            <button
                                                type="button"
                                                className="btn btn-danger ms-1"
                                                onClick={()=>{
                                                    if(window.confirm("確定要刪除這筆資料嗎?")){
                                                        handleDeleteInventory(item.id);
                                                    }
                                                }}>
                                                <i className="bi bi-trash"></i>
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
                {/* 庫存內容下方選項 */}
                <div className="mt-2">
                    {/* 新增庫存btn */}
                    <button
                        type="button"
                        className="btn btn-primary"
                        onClick={()=>setShowAddInventoryModal(true)}>
                        <i className="bi bi-plus-circle"></i> 新增庫存
                    </button>
                </div>
            </div>

            {/* 新增庫存Modal */}
            <Modal
                show={showAddInventoryModal}
                onClose={()=>{
                    setShowAddInventoryModal(false);
                    setAddNewInventory({});
                }}
                title="新增庫存"
                onConfirm={()=>{
                    handleAddInventory();
                }}
                confirmBtnChildren="新增"
                closeBtnChldren="取消">
                <div style={{height: "400px", overflowY: "scroll"}}>
                    <form id="addForm" className="needs-validation" noValidate>
                        {/* 圖片 */}
                        <div className="mb-3">
                            <label htmlFor="img_path" className="form-label">圖片</label>
                            <div className="d-flex gap-2">
                                <input 
                                    type="file" 
                                    className="form-control w-50" 
                                    id="img_path" 
                                    accept="image/*" 
                                    onChange={(e)=>{
                                        const file = e.target.files[0];
                                        if(file){
                                            const reader = new FileReader();
                                            reader.onloadend = () => {
                                                setAddNewInventory({...addNewInventory, img_path: reader.result});
                                            };
                                            reader.readAsDataURL(file);
                                        }
                                    }}
                                    required/>
                                <button
                                    type="button"
                                    className="btn btn-danger"
                                    onClick={()=>setAddNewInventory({...addNewInventory, img_path: ""})}>
                                    <i className="bi bi-x-circle"></i> 清除圖片
                                </button>
                            </div>
                            <span className="invalid-feedback">請上傳圖片</span>
                            <img src={addNewInventory?.img_path} alt="預覽圖片" className="img-fluid mt-2"/>
                        </div>
                        {/* 名稱 */}
                        <div className="mb-3">
                            <label htmlFor="name" className="form-label">名稱</label>
                            <input 
                                type="text" 
                                className="form-control" 
                                id="name" 
                                placeholder="請輸入名稱" 
                                value={addNewInventory?.name}
                                onChange={(e)=>{
                                    setAddNewInventory({...addNewInventory, name: e.target.value});
                                }}
                                required/>
                            <span className="invalid-feedback">請輸入名稱</span>
                        </div>
                        {/* 類型 */}
                        <div className="mb-3">
                            <label htmlFor="type" className="form-label">類型</label>
                            <select 
                                className="form-select" 
                                onChange={(e)=>{
                                    setAddNewInventory({...addNewInventory, type: e.target.value});
                                }}
                                required>
                                <option value="" selected disabled>請選擇類型</option>
                                {
                                    [
                                        {title: "海鮮泡麵", value: "seafood"},
                                        {title: "辛辣泡麵", value: "spicy"},
                                        {title: "素食泡麵", value: "vegetarian"},
                                    ].map((item, index)=>(
                                        <option key={index} value={item.value}>{item.title}</option>
                                    ))
                                }
                            </select>
                            <span className="invalid-feedback">請選擇類型</span>
                        </div>
                        {/* 描述 */}
                        <div className="mb-3">
                            <label htmlFor="description" className="form-label">描述</label>
                            <textarea 
                                className="form-control" 
                                id="description" 
                                rows="3"
                                placeholder="請輸入描述"
                                value={addNewInventory?.description}
                                onChange={(e)=>{
                                    setAddNewInventory({...addNewInventory, description: e.target.value});
                                }}
                                required>
                            </textarea>
                            <span className="invalid-feedback">請輸入描述</span>
                        </div>
                        {/* 成本 */}
                        <div className="mb-3">
                            <label htmlFor="cost" className="form-label">成本</label>
                            <input 
                                type="number" 
                                className="form-control" 
                                id="cost" 
                                placeholder="請輸入成本" 
                                value={addNewInventory?.cost}
                                onChange={(e)=>{
                                    setAddNewInventory({...addNewInventory, cost: e.target.value});
                                }}
                                required/>
                            <span className="invalid-feedback">請輸入成本</span>
                        </div>
                        {/* 售價 */}
                        <div className="mb-3">
                            <label htmlFor="price" className="form-label">售價</label>
                            <input 
                                type="number" 
                                className="form-control" 
                                id="price" 
                                placeholder="請輸入售價" 
                                value={addNewInventory?.price}
                                onChange={(e)=>{
                                    setAddNewInventory({...addNewInventory, price: e.target.value});
                                }}
                                required/>
                                <span className="invalid-feedback">請輸入售價</span>
                        </div>
                        {/* 庫存 */}
                        <div className="mb-3">
                            <label htmlFor="quantity" className="form-label">庫存</label>
                            <input 
                                type="number" 
                                className="form-control" 
                                id="quantity" 
                                placeholder="請輸入庫存數量" 
                                value={addNewInventory?.quantity}
                                onChange={(e)=>{
                                    setAddNewInventory({...addNewInventory, quantity: e.target.value});
                                }}
                                required/>
                                <span className="invalid-feedback">請輸入庫存數量</span>
                        </div>
                    </form>
                </div>
            </Modal>

            {/* 編輯庫存Modal */}
            <Modal
                show={showEditInventoryModal}
                title="編輯庫存"
                onClose={()=>{
                    setShowEditInvemtoryModal(false);
                    setEditInventory({});
                }}
                onConfirm={()=>{
                    handleEditInventory();
                }}
                closeBtnChldren="取消"
                confirmBtnChildren="確認">
                <div style={{height: "400px", overflowY: "scroll"}}>
                    <form id="editForm" className="needs-validation" noValidate>
                        {/* 圖片 */}
                        <div className="mb-3">
                            <label htmlFor="img_path" className="form-label">圖片</label>
                            <div className="d-flex gap-2">
                                <input 
                                    type="file" 
                                    className="form-control w-50" 
                                    id="img_path" 
                                    accept="image/*" 
                                    onChange={(e)=>{
                                        const file = e.target.files[0];
                                        if(file){
                                            const reader = new FileReader();
                                            reader.onloadend = () => {
                                                setEditInventory({...editInventory, new_img_path: reader.result});
                                            };
                                            reader.readAsDataURL(file);
                                        }
                                    }}
                                    required={!editInventory?.img_path && !editInventory?.new_img_path}/>
                                <button
                                    type="button"
                                    className="btn btn-danger"
                                    onClick={()=>setEditInventory({...editInventory, new_img_path: ""})}>
                                    <i className="bi bi-x-circle"></i> 清除圖片
                                </button>
                            </div>
                            <span className="invalid-feedback">請上傳圖片</span>
                            <img src={
                                editInventory?.new_img_path ? (editInventory?.new_img_path) : (`https://localhost:7220/${editInventory?.img_path}`)
                            } alt="預覽圖片" className="img-fluid mt-2"/>
                        </div>
                        {/* 名稱 */}
                        <div className="mb-3">
                            <label htmlFor="name" className="form-label">名稱</label>
                            <input 
                                type="text" 
                                className="form-control" 
                                id="name" 
                                placeholder="請輸入名稱" 
                                value={editInventory?.name}
                                onChange={(e)=>{
                                    setEditInventory({...editInventory, name: e.target.value});
                                }}
                                required/>
                            <span className="invalid-feedback">請輸入名稱</span>
                        </div>
                        {/* 類型 */}
                        <div className="mb-3">
                            <label htmlFor="type" className="form-label">類型</label>
                            <select 
                                className="form-select" 
                                onChange={(e)=>{
                                    setEditInventory({...editInventory, type: e.target.value});
                                }}
                                required>
                                {
                                    [
                                        {title: "海鮮泡麵", value: "seafood"},
                                        {title: "辛辣泡麵", value: "spicy"},
                                        {title: "素食泡麵", value: "vegetarian"},
                                    ].map((item, index)=>(
                                        <option key={index} value={item.value} selected={editInventory?.type == item.value}>{item.title}</option>
                                    ))
                                }
                            </select>
                            <span className="invalid-feedback">請選擇類型</span>
                        </div>
                        {/* 描述 */}
                        <div className="mb-3">
                            <label htmlFor="description" className="form-label">描述</label>
                            <textarea 
                                className="form-control" 
                                id="description" 
                                rows="3"
                                placeholder="請輸入描述"
                                value={editInventory?.description}
                                onChange={(e)=>{
                                    setEditInventory({...editInventory, description: e.target.value});
                                }}
                                required>
                            </textarea>
                            <span className="invalid-feedback">請輸入描述</span>
                        </div>
                        {/* 成本 */}
                        <div className="mb-3">
                            <label htmlFor="cost" className="form-label">成本</label>
                            <input 
                                type="number" 
                                className="form-control" 
                                id="cost" 
                                placeholder="請輸入成本" 
                                value={editInventory?.cost}
                                onChange={(e)=>{
                                    setEditInventory({...editInventory, cost: e.target.value});
                                }}
                                required/>
                            <span className="invalid-feedback">請輸入成本</span>
                        </div>
                        {/* 售價 */}
                        <div className="mb-3">
                            <label htmlFor="price" className="form-label">售價</label>
                            <input 
                                type="number" 
                                className="form-control" 
                                id="price" 
                                placeholder="請輸入售價" 
                                value={editInventory?.price}
                                onChange={(e)=>{
                                    setEditInventory({...editInventory, price: e.target.value});
                                }}
                                required/>
                                <span className="invalid-feedback">請輸入售價</span>
                        </div>
                        {/* 庫存 */}
                        <div className="mb-3">
                            <label htmlFor="quantity" className="form-label">庫存</label>
                            <input 
                                type="number" 
                                className="form-control" 
                                id="quantity" 
                                placeholder="請輸入庫存數量" 
                                value={editInventory?.quantity}
                                onChange={(e)=>{
                                    setEditInventory({...editInventory, quantity: e.target.value});
                                }}
                                required/>
                                <span className="invalid-feedback">請輸入庫存數量</span>
                        </div>
                    </form>
                </div>
            </Modal>
        </>
    )
}