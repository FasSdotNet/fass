﻿import React, { Component } from "react";
import MyInput from "../form/MyInput"
import MySubmit from "../form/MySubmit"


export class ProjectCreateComponent extends Component {

    constructor(props) {
        super(props);
        this.handleSubmit = this.handleSubmit.bind(this);
    }

    handleSubmit(event) {
        const projectName = this.refs.projectName.state.value;
        const projectDesc = this.refs.projectDescription.state.value;
        var result = fetch('/api/v1.0/projects', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            credentials: "same-origin",
            body: JSON.stringify({
                ProjectName: projectName,
                Description: projectDesc
            })
        });

        result.then( (res) =>  {
            if (res.ok) {
                res.json()
                    .then((js) => {
                        document.location.href ="/#/projects";
                    });
            }
        });
    }

    handleCancel(event) {
        document.location.href = "/#/projects";
    }

    render() {
        return (
            <div className="form-horizontal">
                <MyInput ref="projectName" id="projectName" label="Project Name"/>
                <MyInput ref="projectDescription" id="projectDescription" label="Description"/>
                <br/>
                <input type="button" 
                        id="submitButton"
                        onClick={this.handleSubmit}
                        value="Create" 
                        className="btn btn-primary col-md-offset-5"/>

                <input type="button" 
                        id="cancelButton"
                        onClick={this.handleCancel}
                        value="Cancel" 
                        className="btn btn-default"/>
            </div>
        );
    }
}

export default ProjectCreateComponent;