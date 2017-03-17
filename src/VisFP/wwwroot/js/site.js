// Write your Javascript code.
var container;
var network;
var options = {
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
            if (data.from == data.to) {
                var r = confirm("Do you want to connect the node to itself?");
                if (r != true) {
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

function buildGraph(graph) {
    container = document.getElementById('mynetwork');

    var data = {
        nodes: new vis.DataSet(graph.nodes),
        edges: new vis.DataSet(graph.edges)
    };
    network = new vis.Network(container, data, options);
}

function objToArray(obj) {
   return $.map(obj, function (value, index) { return [value] })
}

function saveGraph() {
    

    var dn = objToArray(network.body.data["nodes"]._data);
    var de = objToArray(network.body.data["edges"]._data);
    var data = {
        graph: JSON.stringify({
            nodes: dn,
            edges: de
        })
    }
    $.ajax({
        url: "/Home/SaveGraph",
        type: "POST",
        dataType: "json",  
        data: data
    })
}

