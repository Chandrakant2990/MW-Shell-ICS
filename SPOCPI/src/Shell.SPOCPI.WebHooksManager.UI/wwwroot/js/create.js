'use strict';

var SPOCPI = SPOCPI || {};

SPOCPI.CreateSubscription = {
    init: function () {
        $(document).ready(function () {
            try {
                SPOCPI.CreateSubscription.outputQueueAutoComplete();
            } catch (e) {
                console.error(e);
            }
            var valueToRestrict = $('input[name=ConfidentialSite]').val();

            $("input[name$='createType']").click(function () {
                var createType = $(this).val();

                if (createType === "bulk") {
                    $("#individualCreate").hide();
                    $("#bulkUploadForm").show();
                }
                else {
                    $("#bulkUploadForm").hide();
                    $("#individualCreate").show();
                }
            });

            $("#createBulk").click(function () {
                event.preventDefault();
                var regex = /^([a-zA-Z0-9\s_\\.\-:])+(.csv)$/;
                if (regex.test($("#inputFile").val().toLowerCase())) {
                    if (typeof FileReader !== "undefined") {
                        var reader = new FileReader();
                        reader.onload = function (e) {
                            var validateExcelData = true;
                            var subscriptions = new Array();
                            var rows = e.target.result.split("\r\n");
                            var InvalidInputMessage = '';
                            for (var i = 1; i < rows.length; i++) {
                                if (rows[i]) {
                                    var cells = SPOCPI.CreateSubscription.csvRowToArray(rows[i]);
                                    if (!cells[0] && !cells[1] && !cells[2] && !cells[4]) {
                                        // no entry is present at the row level
                                        continue;
                                    }
                                    if (cells.length > 1 && cells[0] && cells[1] && cells[2] && cells[4] && cells[5]) {
                                        var subscription = {};
                                        subscription.SiteUrl = SPOCPI.Common.stripTrailingSlash(cells[0]);
                                        subscription.LibraryUrl = cells[1];
                                        subscription.OutputQueue = cells[2];
                                        subscription.Description = cells[3];
                                        subscription.Parameters = cells[4];

                                        // convert string into boolean values for IsActive and AutoIndex columns                                        
                                        var isActiveBoolValue = SPOCPI.Common.stringToBoolean(cells[5]);
                                        if (isActiveBoolValue !== "InValidInput") {
                                            subscription.IsActive = isActiveBoolValue;
                                        } else {
                                            InvalidInputMessage += '<p>"IsActive" column contains invalid boolean value for LibraryUrl: ' + subscription.LibraryUrl + '</p>';
                                        }

                                        var autoIndexBoolValue = SPOCPI.Common.stringToBoolean(cells[6]);
                                        if (autoIndexBoolValue !== "InValidInput") {
                                            subscription.AutoIndex = autoIndexBoolValue;
                                        } else {
                                            InvalidInputMessage += '<p>"AutoIndex" column contains invalid boolean value for LibraryUrl: ' + subscription.LibraryUrl + '</p>';
                                        }
                                        if (SPOCPI.Common.checkForConfidentialSite(subscription.SiteUrl,valueToRestrict)) {
                                            InvalidInputMessage += '<p>"SiteUrl" column contains confidential site: ' + subscription.SiteUrl + '</p>';
                                        }
                                        //// check for any html input
                                        if (SPOCPI.Common.checkForValidInput(subscription.SiteUrl) &&
                                            SPOCPI.Common.checkForValidInput(subscription.LibraryUrl) &&
                                            SPOCPI.Common.checkForValidInput(subscription.Description) &&
                                            SPOCPI.Common.checkForValidInput(subscription.Parameters) &&
                                            SPOCPI.Common.checkForValidInput(subscription.OutputQueue)) {
                                            subscriptions.push(subscription);
                                        } else {
                                            $('#dangerModal').modal({
                                                keyboard: false
                                            });
                                            $('.modal-body').html("Invalid input detected");
                                        }
                                    } else {
                                        validateExcelData = false;
                                        var message = 'Bulk upload failed:\n';
                                        if (!cells[0]) {
                                            message += ' "Site Url" column cannot be empty.\n';
                                        }
                                        if (!cells[1]) {
                                            message += ' "Library Url" column cannot be empty.\n';
                                        }
                                        if (!cells[2]) {
                                            message += ' "Activity Type" column cannot be empty.\n';
                                        }
                                        if (!cells[4]) {
                                            message += ' "IsActive" column cannot be empty.';
                                        }
                                        if (!cells[5]) {
                                            message += ' "Auto Index" column cannot be empty';
                                        }
                                        if (message) {
                                            $('#dangerModal').modal({
                                                keyboard: false
                                            });
                                            $('.modal-body').html(message);
                                        }
                                    }
                                }
                            }

                            if (InvalidInputMessage) {
                                $('#dangerModal').modal({
                                    keyboard: false
                                });
                                $('.modal-body').html("<p>Bulk upload failed:\n" + InvalidInputMessage + "</p>");
                                $('#bulkUploadForm input[type="file"]').val(null);
                            } else {

                                if (subscriptions && subscriptions.length > 0 && subscriptions.length <= 1000 && validateExcelData) {
                                    SPOCPI.CreateSubscription.postSubscriptions(subscriptions);
                                } else if (subscriptions && subscriptions.length > 1000) {
                                    $('#dangerModal').modal({
                                        keyboard: false
                                    });
                                    $('.modal-body').html("Bulk upload does not support more than 1000 items");
                                }
                            }
                        };
                        reader.readAsText($("#inputFile")[0].files[0]);
                    } else {
                        $('#dangerModal').modal({
                            keyboard: false
                        });
                        $('.modal-body').html("This browser does not support HTML5.");
                    }
                } else {
                    $('#dangerModal').modal({
                        keyboard: false
                    });
                    $('.modal-body').html("Please upload a valid CSV file.");
                }
            });

            jQuery.validator.setDefaults({
                debug: true,
                success: "valid"
            });

            $.validator.addMethod('urlValidator', function (value, element, params) {
                var siteUrl = SPOCPI.Common.stripTrailingSlash($('input[name="' + params[0] + '"]').val()),
                    libraryUrl = $('input[name="' + params[1] + '"]').val();
                return (libraryUrl.substring(0, libraryUrl.lastIndexOf('/')).toLowerCase() == siteUrl.toLowerCase());
            }, "The library doesn't lie directly under the given site, please enter valid urls"
            );
            $.validator.addMethod('confidentialSiteValidator', function (value, element) {
                return (!SPOCPI.Common.checkForConfidentialSite(value, valueToRestrict)) 
            }, "This site is confidential. access to this is restricted"
            );
            $("#individualCreate").validate({
                rules: {
                    SiteUrl: {
                        required: true,
                        url: true,
                        confidentialSiteValidator: true
                    },
                    LibraryUrl: {
                        required: true,
                        url: true,
                        urlValidator: ['SiteUrl', 'LibraryUrl'],
                    },
                    Description: {
                        required: true
                    }
                },
                messages: {
                    LibraryUrl: { required: "Please enter a valid library url", url: "Please enter a valid library url", urlValidator: "The library doesn't lie directly under the given site, please enter valid urls" },
                    SiteUrl: { required: "Please enter a valid site url", url: "Please enter a valid site url", urlValidator: "The library doesn't lie directly under the given site, please enter valid urls", confidentialSiteValidator: "This site is confidential . access to this is restricted"},
                    Description: { required: "Please enter a description" }
                },
                errorElement: "em",
                errorPlacement: function (error, element) {
                    // Add the `help-block` class to the error element
                    error.addClass("help-block");

                    if (element.prop("type") === "checkbox") {
                        error.insertAfter(element.parent("label"));
                    } else {
                        error.insertAfter(element);
                    }
                },
                highlight: function (element, errorClass, validClass) {
                    $(element).parents(".col-sm-5").addClass("has-error").removeClass("has-success");
                },
                unhighlight: function (element, errorClass, validClass) {
                    $(element).parents(".col-sm-5").addClass("has-success").removeClass("has-error");
                }
            });

			$("#createSub").click(function () {
				var $btn = $(this);
				$btn.button('loading');
				event.preventDefault();
				if ($("#individualCreate").valid()) {
					var subscriptions = [{
                        SiteUrl: SPOCPI.Common.stripTrailingSlash($("#SiteUrlInput").val()),
						LibraryUrl: $("#LibraryUrlInput").val(),
						IsActive: Boolean($("#IsActive")[0].checked),
						AutoIndex: Boolean($("#AutoIndex")[0].checked),
						Description: $("#DescriptionInput").val(),
                        OutputQueue: $("#queueNamesAutoComplete").val(),
                        Parameters: $("#ParametersInput").val(),
						IncludeFolderRelativePath: $("#IncludeFolderRelativePathInput").val()
					}];
					if (SPOCPI.Common.checkForValidInput(subscriptions.SiteUrl) &&
						SPOCPI.Common.checkForValidInput(subscriptions.LibraryUrl) &&
                        SPOCPI.Common.checkForValidInput(subscriptions.Description) &&
                        SPOCPI.Common.checkForValidInput(subscriptions.Parameters) &&
						SPOCPI.Common.checkForValidInput(subscriptions.OutputQueue)) {
						SPOCPI.CreateSubscription.postSubscriptions(subscriptions);
					} else {
						$('#dangerModal').modal({
							keyboard: false
						});
						$('.modal-body').html("Invalid input detected");
					}
				}
				$btn.button('reset');
			});
		});
		$('#successModal').on('hidden.bs.modal', function (e) {
			location.href = "/sub/create";
		});
	},
	postSubscriptions: function (subscriptions) {
		$.ajax({
			type: "POST",
			url: "/api/subscriptions",
			headers: {
				"X-CSRF-TOKEN-SPOCPI": $('input[name="RequestVerificationToken"]').val()
			},
			data: JSON.stringify(subscriptions),
			dataType: "json",
			contentType: 'application/json; charset=utf-8',
			success: function (data) {
				$('#successModal').modal({
					keyboard: false
				});
				$('.modal-body').html("Your subscription request is added to the queue.");
			},
			error: function (xhr, textStatus, errorThrown) {
				try {
					var message = xhr.responseText;
					if (!message) {
						message = "You don't have permission to perform this action";
					}
					$('#dangerModal').modal({
						keyboard: false
					});
					$('.modal-body').html(message);
				} catch (e) {
					console.error(e);
				}
			}
		});
    },
    csvRowToArray: function (row, delimiter = ',', quoteChar = "'") {
        row = row.replace(/(\"\"\")/g, "'");
        let nStart = 0, nEnd = 0, a = [], nRowLen = row.length, bQuotedValue;
        while (nStart <= nRowLen) {
            bQuotedValue = (row.charAt(nStart) === quoteChar);
            if (bQuotedValue) {
                nStart++;
                nEnd = row.indexOf(quoteChar + delimiter, nStart)
            } else {
                nEnd = row.indexOf(delimiter, nStart)
            }
            if (nEnd < 0) nEnd = nRowLen;
            a.push(row.substring(nStart, nEnd).replace(/""/g, '"').replace(/'\"/g, '""'));
            nStart = nEnd + delimiter.length + (bQuotedValue ? 1 : 0)
        }
        return a;
    },
	split: function (val) {
		return val.split(/;\s*/);
	},
	outputQueueAutoComplete: function () {
		$.ajax({
			url: "/api/subscriptions/autocompleteSuggestions",
			type: 'GET',
			dataType: 'json',
			contentType: 'application/json; charset=utf-8',
			success: function (data) {
				if (data) {
					$("#queueNamesAutoComplete").autocomplete({
						source: data,
						minLength: 0,
						select: function (event, ui) {
							$(this).val(ui.item.value);
						}
					});
				}
			},
			error: function (xhr, textStatus, errorThrown) {
				try {
					var message = xhr.responseText;
					if (message) {
						$('#dangerModal').modal({
							keyboard: false
						});
						$('.modal-body').html(message);
					}
				} catch (e) {
					console.error(e);
				}
			}
		});
    }
};

SPOCPI.CreateSubscription.init();