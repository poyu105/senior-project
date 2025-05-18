//Card參數: imgPath:圖片路徑, title:標題, chlidren:內容, onClickFunc: 點擊函數
export default function Card({ imgPath, title, children, onClickFunc, showImg=false, cardHeight="100%" }) {
    return(
        <>
            <div 
                className="card"
                style={cardHeight ? { height: cardHeight } : {}}
                onClick={()=>onClickFunc()}>
                {
                    showImg && (
                        <img
                            className="img-fluid m-1"
                            style={{ height: "180px", objectFit: "cover" }}
                            src={imgPath}
                            alt="image"
                        />
                    )
                }
                <div className="card-body">
                    <h4 className="card-title">{title}</h4>
                    {children}
                </div>
            </div>
        </>
    )
}