//AjaxPagerX 分页控件跳转用于跳转 传入form名称和页码
function AjaxToPage(formname, pageindex) {

    var _pageIndex = '#' + formname + ' #pageIndex';
    $(_pageIndex).val(pageindex);

    if ($(_pageIndex).length == 0) {
        for (var i = 0; i < document.getElementsByName("pageIndex").length; i++) {
            document.getElementsByName("pageIndex")[i].value = pageindex;
        }
    }

    if ($("#" + formname).length != 0) {
        $('#' + formname).submit();
    } else {
        $("[data-ajax-update='#divreplace']").submit();
    }
  
    return false;
}

//AjaxPagerX 分页控件跳转用于刷新当前页      
function AjaxToRefresh(formname) {
    var _pageIndex = '#' + formname + ' #pageIndex';
    var _HpageIndex = '#' + formname + ' #HpageIndex';
    $(_pageIndex).val($(_HpageIndex).val());
    $('#' + formname).submit();
    return false;
}

//AjaxPagerX 分页控件用于改变form里面隐藏域的值，如果没有将创建一个
function UpdateHidden(formname, hiddenName, value) {
    var temp_hiddenName = '#' + formname + ' #' + hiddenName;

    if ($("#" + formname).length == 0) {

        if ($("[data-ajax-update='#divreplace'] input[name='" + hiddenName + "']").length > 0) {

            $("[data-ajax-update='#divreplace'] input[name='" + hiddenName + "']").val(value);
        } else {
            var str = '<input  type=\"hidden\" id=\"' + hiddenName + '\" name=\"' + hiddenName + '\" value=\"' + value + '\"  /> ';
            $("[data-ajax-update='#divreplace']").append(str);
        }
    } else {

        if ($(temp_hiddenName).length > 0 || (isie6() && ($("#" + formname + " input[name='" + hiddenName + "']").length > 0))) {
            $(temp_hiddenName).val(value);
        }
        else {
            var str = '<input  type=\"hidden\" id=\"' + hiddenName + '\" name=\"' + hiddenName + '\" value=\"' + value + '\"  /> ';
            $('#' + formname).append(str);
        }
    }
}


function documentCheck() {
    ///<summary>
    /// 用于在通过AJAX的形式取得新的页面内容之后,重新刷新页面信息以便实现验证
    ///</summary>

    $.validator.unobtrusive.parse(document);
}



//数组删除元素 传入数组下标
Array.prototype.remove = function (dx) {
    if (isNaN(dx) || dx > this.length) { return false; }
    this.splice(dx, 1);
    return this;
}


//webFileUrl 请求地址
//jsonArgs参数 {}
//callBackFunc 回调 
//是否异步async 默认true
//是否开启遮罩  默认true
$.AjaxServer = function (webFileUrl, jsonArgs, callBackFunc, async, isTip) {
    if (isTip == null) {
        isTip = true;
    }
    try {
        $.ajax({
            url: webFileUrl,
            data: jsonArgs,
            type: 'Post',
            async: async == null ? true : async,
            cache: false,
            success: function (data, textStatus, rqinfo) {
                if (data != null && typeof (data) == "object" && typeof (data.Status) != "undefined" && data.Status == 0) {
                    if ($.alert == undefined) {
                        alert(data.ErrorSimple);
                    } else {
                        $.alert(data.ErrorSimple);
                    }
                } else {
                    callBackFunc(data, textStatus, rqinfo);
                }
            },
            error: function (data, textStatus, rqinfo) {
                callBackFunc(data, textStatus, rqinfo);
            },
            complete: function (XHRequest, T) {
                XHRequest = null
            },
            isTip: isTip
        });
    }
    catch (e) {
        alert(e)
    }
}




//用于BooleanType控件切换是否
function BooleanTypeChange(obj) {
    var id = "#" + $(obj).attr("forid");
    $(id).val($(obj).is(':checked') ? "1" : "0");
}

//判断浏览器是否是ie6
function isie6() {

    return false;
}

/****************全局遮罩******************/
$(document).ajaxSend(ajaxTips).ajaxComplete(unTips).ajaxError(unTips);

function ajaxTips(event, xhr, settings) {
    if (settings.isTip != null && settings.isTip == false) {
        return;
    }

    settings._ajaxartDialogClose = false;
    $.blockUI({ message: null });

}

function unTips(event, xhr, settings) {
    if (settings._ajaxartDialogClose != null) {
        setTimeout($.unblockUI, 1000);
    }

    if (typeof dataSrc != "undefined") {
        dataSrc();
    }
}
/****************全局遮罩End******************/


//页面加载完成时执行
function PagerInfo() {
    documentCheck();
}


$(PagerInfo)


/****************对话框******************/

//funok可以是事件或者跳转地址
$.confirm = function (content, funok) {
    var confirm = $.scojs_confirm({
        content: content,
        action: function () {
            funok();
            this.close();
            this.destroy();
        }
    });
    confirm.show();
}

$.alert = function (message) {
    var alert = $.scojs_modal({
        title: message,
        content: '<div class="modal-footer"><a class="btn cancel" href="#" data-dismiss="modal">Ok</a></div>'
    });
    alert.show();
}


$.tipsOk = function (message) {
   $.scojs_message(message, $.scojs_message.TYPE_OK);
    return;
    var sb = "";
  //  sb +=("  <div class=\'modal-dialog modal-sm\' role = \'document\' >");
    sb +=("        <div class=\'modal-content\'>");
    sb +=("            <div class=\'modal-status bg-success\'></div>");
    sb += ("            <div class=\'modal-body text-center py-4\'>");
    sb +=("<svg xmlns=\'http://www.w3.org/2000/svg\' class=\'icon mb-2 text-green icon-lg\' width=\'24\' height=\'24\' viewBox=\'0 0 24 24\' stroke-width=\'2\' stroke=\'currentColor\' fill=\'none\' stroke-linecap=\'round\' stroke-linejoin=\'round\'><path stroke=\'none\' d=\'M0 0h24v24H0z\' fill=\'none\'></path><circle cx=\'12\' cy=\'12\' r=\'9\'></circle><path d=\'M9 12l2 2l4 -4\'></path></svg>");
    sb +=("      <h3>Succedeed</h3>");
    sb += ("                <div class=\'text-muted\'>" + message+"</div>");
    sb +=("            </div>");
    sb +=("           ");
    sb +=("        </div>");
  //  sb +=("</div >");


    var sb = '  ';
    sb += '    <div class="modal-status bg-success"></div>';
    sb += '    <div class="modal-body text-center py-4">';
    sb += ("<svg xmlns=\'http://www.w3.org/2000/svg\' class=\'icon mb-2 text-green icon-lg\' width=\'48\' height=\'48\' viewBox=\'0 0 24 24\' stroke-width=\'2\' stroke=\'currentColor\' fill=\'none\' stroke-linecap=\'round\' stroke-linejoin=\'round\'><path stroke=\'none\' d=\'M0 0h24v24H0z\' fill=\'none\'></path><circle cx=\'12\' cy=\'12\' r=\'9\'></circle><path d=\'M9 12l2 2l4 -4\'></path></svg>");

    sb += '        <div class="text-muted">' + message + '</div>';
    sb += '    </div>';
    sb += '    <div class="modal-footer">';
    sb += '    </div>';
    tipsErrorCol = $.scojs_modal({
        content: sb,
        title: 'success'
    }).show();


    setTimeout(function () { tipsErrorCol.close() }, 3000);
}

var tipsErrorCol = null;
$.tipsError = function (message) {
    $.scojs_message(message, $.scojs_message.TYPE_ERROR);

    return;
    var sb = '  ';
    sb += '    <div class="modal-status bg-danger"></div>';
    sb += '    <div class="modal-body text-center py-4">';
    sb += '        <svg xmlns="http://www.w3.org/2000/svg" class="icon mb-2 text-danger icon-lg" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round"><path stroke="none" d="M0 0h24v24H0z" fill="none"></path><path d="M12 9v2m0 4v.01"></path><path d="M5 19h14a2 2 0 0 0 1.84 -2.75l-7.1 -12.25a2 2 0 0 0 -3.5 0l-7.1 12.25a2 2 0 0 0 1.75 2.75"></path></svg>';

    sb += '        <div class="text-muted">' + message + '</div>';
    sb += '    </div>';
    sb += '    <div class="modal-footer">';
    sb += '    </div>';
    tipsErrorCol= $.scojs_modal({
        content: sb,
        title: 'error'
   }).show();
    setTimeout(function () { tipsErrorCol.close() }, 3000);
}


  


/****************全局遮罩End******************/


function OnConfirm(obj, onajax) {

    try {
        var col = $(obj);
        var onAction = function () {
            var button = this;
            $.AjaxServer(col.attr("url"), {}, function (obj) {
                eval(onajax)(obj);
                button.close();
                button.destroy();
            });
        };
        var confirm = $.scojs_confirm({
            content: col.attr("data-title"),
            action: onAction
        });
        confirm.show();
    } catch (e) {

    }
    return false;
}



$(function () {
    dataSrc();
})



function dataSrc() {

    $("[data-src]").each(function (p, obj) {
        $(obj).unbind("click");
        $(obj).click(function () {

            var title = $(this).attr("title");
            if (title == undefined) {
                title = "对话框";
            }
     
            $.AjaxServer($(this).attr("data-src"), {}, function (content) {
                var acontent = $.scojs_modal({
                    title: title,
                    content: content
                });
                acontent.show();
                documentCheck();
            });
            return false;
        })
    })

    $("[data-confirm]").each(function (p, obj) {
        $(obj).unbind("click");
        $(obj).click(function () {

            var title = $(this).attr("title");
            if (title == undefined) {
                alert("对话框未设置提示");
                return false;
            }
            var col = $(obj);
          
            var sb = '   <button type="button" class="btn-close" data-dismiss="modal" aria-label="Close"></button>';
            sb += '    <div class="modal-status bg-danger"></div>';
            sb += '    <div class="modal-body text-center py-4">';
            sb += '        <svg xmlns="http://www.w3.org/2000/svg" class="icon mb-2 text-danger icon-lg" width="24" height="24" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round"><path stroke="none" d="M0 0h24v24H0z" fill="none"></path><path d="M12 9v2m0 4v.01"></path><path d="M5 19h14a2 2 0 0 0 1.84 -2.75l-7.1 -12.25a2 2 0 0 0 -3.5 0l-7.1 12.25a2 2 0 0 0 1.75 2.75"></path></svg>';
    
            sb += '        <div class="text-muted">' + title+'</div>';
            sb += '    </div>';
            sb += '    <div class="modal-footer">';
            sb += '        <div class="w-100">';
            sb += '            <div class="row">';
            sb += '                <div class="col"><a href="#" class="btn btn-white w-100" data-dismiss="modal">';
            sb += '                    取消';
            sb += '                  </a></div>';
            sb += '                <div class="col"><a href="#" data-action-confirm="' + col.attr("data-confirm")+'" class="btn btn-danger w-100" data-bs-dismiss="modal">';
            sb += '                    确定';
            sb += '                  </a></div>';
            sb += '            </div>';
            sb += '        </div>';
            sb += '    </div>';
            buttonconfirm = $.scojs_modal({
                content: sb,
                title:'Are you sure?'
            });
            buttonconfirm.show();
            dataconfirm();
            return false;
        })
    })
}

var buttonconfirm = null;

function dataconfirm() {

    $("[data-action-confirm]").each(function (p, obj) {
        $(obj).unbind("click");
        $(obj).click(function () {
            $.AjaxServer($(this).attr("data-action-confirm"), {}, function (data) {
               
                if (data) {
                    // ReturnValue的返回结果
                    if (data.status != null && data.status != undefined) {
                        if (data.status == true) {
                            $.tipsOk("操作成功!");
                        } else {
                            if (data.errorsimple)
                                $.tipsError(data.errorsimple);
                            else
                                $.tipsError('eRROR');
                        }
                    } else {
                        $.tipsOk("操作成功!");
                    }
                    setTimeout(function () {
                        window.location.reload();
                    }, 1500);
                }
                else {
                    $.tipsError('eRROR');
                }
                if (buttonconfirm != null) {
                    buttonconfirm.close();
                    buttonconfirm.destroy();
                }
               
            });
            return false;
        })
    })
    return false;
}

var ajaxRvCompleted = function (data) {
    if (data.status != 200) {
        $.tipsError(data.responseText);
        return;
    }
    if (data.responseJSON != false && data.responseJSON.status) {
        $.tipsOk("操作成功!");
        setTimeout(function () {
            window.location.reload();
        }, 1500);
    }
    else {
        $.tipsError(data.responseJSON.errorsimple);
    }
};





 