// Write your Javascript code.
var container;
var network;
var options = {
    nodes: { borderWidth: 2 },
    manipulation: {
        addNode: function (data, callback) {
            // filling in the popup DOM elements
            document.getElementById('node-operation').innerHTML = "Add Node";
            editNode(data, callback);
        },
        editNode: function (data, callback) {
            // filling in the popup DOM elements
            document.getElementById('node-operation').innerHTML = "Edit Node";
            editNode(data, callback);
        },
        addEdge: function (data, callback) {
            if (data.from === data.to) {
                var r = confirm("Do you want to connect the node to itself?");
                if (r !== true) {
                    callback(null);
                    return;
                }
            }
            document.getElementById('edge-operation').innerHTML = "Add Edge";
            editEdgeWithoutDrag(data, callback);
        },
        editEdge: {
            editWithoutDrag: function (data, callback) {
                document.getElementById('edge-operation').innerHTML = "Edit Edge";
                editEdgeWithoutDrag(data, callback);
            }
        }
    }
};
onload = function () {

    $('#radioBtn a').on('click', function () {
        var sel = $(this).data('title');
        var tog = $(this).data('toggle');
        $('#' + tog).prop('value', sel);

        $('a[data-toggle="' + tog + '"]').not('[data-title="' + sel + '"]').removeClass('active').addClass('notActive');
        $('a[data-toggle="' + tog + '"][data-title="' + sel + '"]').removeClass('notActive').addClass('active');
    })

    $(function () {
        $('.button-checkbox').each(function () {

            // Settings
            var $widget = $(this),
                $button = $widget.find('button'),
                $checkbox = $widget.find('input:checkbox'),
                color = $button.data('color'),
                settings = {
                    on: {
                        icon: 'glyphicon glyphicon-check'
                    },
                    off: {
                        icon: 'glyphicon glyphicon-unchecked'
                    }
                };

            // Event Handlers
            $button.on('click', function () {
                $checkbox.prop('checked', !$checkbox.is(':checked'));
                $checkbox.triggerHandler('change');
                updateDisplay();
            });
            $checkbox.on('change', function () {
                updateDisplay();
            });

            // Actions
            function updateDisplay() {
                var isChecked = $checkbox.is(':checked');

                // Set the button's state
                $button.data('state', (isChecked) ? "on" : "off");

                // Set the button's icon
                $button.find('.state-icon')
                    .removeClass()
                    .addClass('state-icon ' + settings[$button.data('state')].icon);

                // Update the button's color
                if (isChecked) {
                    $button
                        .removeClass('btn-default')
                        .addClass('btn-' + color + ' active');
                }
                else {
                    $button
                        .removeClass('btn-' + color + ' active')
                        .addClass('btn-default');
                }
            }

            // Initialization
            function init() {

                updateDisplay();

                // Inject the icon if applicable
                if ($button.find('.state-icon').length === 0) {
                    $button.prepend('<i class="state-icon ' + settings[$button.data('state')].icon + '"></i> ');
                }
            }
            init();
        });
    });
}

function buildGraph(graph) {
    container = document.getElementById('mynetwork');

    var data = {
        nodes: new vis.DataSet(graph.nodes),
        edges: new vis.DataSet(graph.edges)
    };
    network = new vis.Network(container, data, options);
}

function objToArray(obj) {
    return $.map(obj, function (value, index) { return [value]; });
}

function disableAnswerButton() {
    $('#sendAnswer').prop("disabled", true);
}

function handleAnswer(answerResult) {
    if (answerResult.block !== undefined ) {
        disableAnswerButton();
        console.log("Stop right there, criminal scum!");
    }
    else {
        if (answerResult.isCorrect) {
            disableAnswerButton();
            $("#answerCorrectness").html("Ответ правильный!");
            $("#answerCorrectness").removeClass();
            $("#answerCorrectness").addClass("answer success-answer");
        }
        else {
            if(answerResult.attemptsLeft === 0)
                disableAnswerButton();
            $("#answerCorrectness").html("Ответ неверный.");
            $("#answerCorrectness").removeClass();
            $("#answerCorrectness").addClass("answer fail-answer");
        }
        $("#attemptsCount").html(answerResult.attemptsLeft);
        
    }
} 

function sendUserAnswer() {
    var data;
    if ($("#answerSymbols").length !== 0) {
        data ={ 
            Answer: objToArray(
                $(".answerCheckboxes")
                .filter(function (i, x) { return x.checked; })
                .map(function (i, x) { return x.value })
            ).join(" "),
            TaskId: $('#taskId').val()
        };
    }
    else if ($("#yesNoAnswer").length !== 0)
    {
        data = {
            Answer: $('#yesNoAnswer').val(),
            TaskId: $('#taskId').val()
        };
    }
    else {
        data = {
            Answer: $('#answer').val(),
            TaskId: $('#taskId').val()
        };
    }
    $.ajax({
        type: "POST",
        url: "/RegGram/Answer",
        data: data,
        success: handleAnswer
    });
}


function saveGraph() {
    var dn = objToArray(network.body.data["nodes"]._data);
    var de = objToArray(network.body.data["edges"]._data);
    var data = {
        graph: JSON.stringify({
            nodes: dn,
            edges: de
        })
    };
    $.ajax({
        url: "/RegGram/SaveGraph",
        type: "POST",
        dataType: "json",
        data: data
    });
}

