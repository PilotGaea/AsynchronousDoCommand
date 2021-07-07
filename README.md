# AsynchronousDoCommand 範例專案檔
PilotGaea O’view Map Server AsynchronousDoCommand Plugin

[開發者文件](https://nlscsample.pilotgaea.com.tw/demo/ProgrammingGuide/src/04.ServerSidePlugin/04.2_PluginSample.html#docommand非同步版本)

## Map Server Plugin 介紹：

在O’view Map Server下，共有以下四種類型的外掛可供自訂：

1. DoCommand 自訂操作供使用者呼叫，實作DoCmdBaseClass
2. Account 實作帳號登入功能，實作AccountBaseClass
3. FeatureInfo 提供查詢WMTS圖素屬性，實作FeatureInfoBaseClass
4. Snap鎖點功能，實作SnapBaseClass

以上幾種外掛，DoCommand可多個同時起作用，而Account、FeatureInfo、Snap則同一時間內只啟動一個。

Map Server在啟動或Plugin目錄被重新設定時，會重新開始搜尋指定目錄下的dll檔，根據其實作的BaseClass建立其實體且將其初始化。

在載入dll的過程中如果拋出了例外，當下讀取中的class將被捨棄；各個外掛都是獨立的，即使其中的class名稱重複也不影響讀取，但如果設定的指令重複，後面讀取的會蓋過前面讀取的；同一時間只啟動一個的外掛，後面讀的也是會蓋過前面的。

當DoCommand執行過程中如果拋出了例外，呼叫者會收到HTTP 500 Internal Server Error的回應，並且Map Server會將此例外會寫入log。

如果使用Web做為前端，請在client各個新增圖層處加上proxy引數，目的是為了在呼叫外掛相關功能時能夠讓cookie正確被設定。

>**注意事項：**
>
> 所有的外掛在編譯時都需注意輸出的dll檔是32位元或是64位元，跟安裝的Map Server必須是同樣位元組的版本，否則讀取外掛時會失敗。

## DoCommand 使用方法：

1. 使用MicroSoft Visual Studio 2017(或以上之版本)開啟專案檔。
2. 開始建置專案。(須注意Debug/Release與CPU版本。)
3. 完成後請到專案資料夾中的`\bin\Debug`(或是`\bin\Release`，依建置類型決定)目錄內將檔案複製到安裝目錄下的`plugins`目錄中。
4. 客戶端發送非同步的DoCommand請求的方式與發送同步DoCommand相同，差別在於取得資料時會透過函式回傳的手柄來操作。除了取資料，還有中斷，詢問進度等功能都需要透過手柄。<br/>這邊示範一下JavaScript端的寫法。<br/>首先我們先把伺服器的連線資訊宣告起來放。

```javascript
let ServerUrl = "127.0.0.1";
let ServerPort = 8080;
let ProxyUrl = "http://" + ServerUrl + ":" + ServerPort;
```

為了方便，我們寫一個可以幫我們快速加入按鈕的函式AddButton()。

```javascript
//加入按鈕
function AddButton(text, event, id = ""){
    const button=document.createElement('button');
    button.innerText=text;
    button.onclick=event;
    if(id != ""){
        button.id = id;
    }
    document.body.appendChild(button);
}
```

再來還需要可以在送出非同步DoCommand之後幫我們每隔一秒鐘向server詢問一次進度的函式GetProgress()。

```javascript
function GetProgress(handle){
    function work() {
        let ret = handle.getProgress();
        console.log(ret);
        return ret;
    }
    let itvId = setInterval(() => {
        let ret = work();
        if(parseInt(ret.Progress) >= 100){//進度>=100停止詢問
            clearInterval(itvId);
        }
    }, 1000);//每一秒問一次
    return itvId;//回傳setInterval的Id,讓setInterval能從外面被中斷
}
```

再來我們建立一個Main()，將主要的行為包在裡面。

```javascript
function Main(){
    let handle = null;
    let itvId = null;   
    AddButton("doByHandle",()=>{
        //以手柄方式呼叫docmd
        let param = {};
        handle = ov.DoCommand.doByHandle(
                proxyUrl,
                        "ConnectionTest",
                        param
                );
               //呼叫完成後開始向server問進度
        itvId = getProgress(handle);
    }); 
    AddButton("getResult",()=>{
        //取得手柄docmd資料
        if(handle === null) return; 
        let ret = handle.getResult();
        console.log(ret);
        if(parseInt(ret.Progress) >= 100){
            handle = null;
        }
    });
    AddButton("abort",()=>{
        //中斷手柄docmd作業
        if(itvId === null || handle === null) return;

        let ret = handle.abort();
        console.log(ret);
        if(ret.success){
            clearInterval(itvId);
            console.log("aborted.");
        }
        else{
            console.log("not aborted.");
        }
    });
}
```

可以看到，當呼叫ov.DoCommand.doByHandle()之後，它會回傳一個手柄(handle)。若是需要進行中斷作業，詢問進度或是取得資料的行為，都會透過這個手柄來操作。

最後呼叫一次Main()。

```javascript
Main();
```

這樣Client端也完成了。
