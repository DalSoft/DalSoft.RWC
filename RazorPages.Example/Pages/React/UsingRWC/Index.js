import './Index.css';

import React from 'react';
import ReactDOM from 'react-dom';
import Background from './background.jpg';


const App = (props) =>
{
    return (
        <div>
            <p>React/UsingRWC/Index.js</p>
            <img src={Background} width="100" />
        </div>)
}


ReactDOM.render(<App />, document.getElementById('app'))