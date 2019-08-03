var inputEle = $('.validate-input .input100');
(function ($) {
    "use strict";
    /*====[ Validate ]*/
    

    $('.validate-form').on('submit', function () {
        var check = true;

        for (var i = 0; i < inputEle.length; i++) {
            if (validate(inputEle[i]) == false) {
                showValidate(inputEle[i]);
                check = false;
            }
        }
        return check;
    });

    

    $('.validate-form .input100').each(function () {
        $(this).focus(function () {
            hideValidate(this);
        });
    });

    

    function showValidate(input) {
        var thisAlert = $(input).parent();

        $(thisAlert).addClass('alert-validate');
    }

    function hideValidate(input) {
        var thisAlert = $(input).parent();

        $(thisAlert).removeClass('alert-validate');
    }



})(jQuery);
function validate(input) {
    if ($(input).attr('type') == 'email' || $(input).attr('name') == 'email') {
        if ($(input).val().trim().match(/^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{1,5}|[0-9]{1,3})(\]?)$/) == null) {
            return false;
        }
    }
    else if ($(input).attr('name') == 'IBAN') {
        return IBAN.isValid($(input).val());
    }
    else if ($(input).attr('name') == 'cellphone') {
        if ($(input).val().trim().match(/^\d{5,12}$/) == null) {
            return false;
        }
    }
    else if ($(input).attr('name') == 'password') {
        if ($(input).val().trim().match(/(?=.{8,})/) == null) {
            return false;
        }
    }
    else if ($(input).attr('name') == 'confirm') {
        if ($(input).val() != $(inputEle[inputEle.length - 2]).val() | $(input).val() == "") {
            return false;
        }
    }
    else {
        if ($(input).val().trim() == '') {
            return false;
        }
    }
    return true;
}

function backInVerify() {
    location.href = './index';
}
function nextInCode() {
    $.ajax({
        url: './checkPhoneCode',
        method: "post",
        contentType: 'application/json',
        data: JSON.stringify({
            code: $("#phoneCode").val()
        }),
        success: function (result) {
            if (result == "Success") {
                location.href = "./index";
            } else {
                alert("Invalid code");
            }

        }
    });
}
function backInCode() {
    location.href = './phoneVerify';
}