<?php
    require 'database.php';
    
    $userId = $_POST['id'];

    if(isset($userId) == false){
    echo 'data struct error';
    exit;
    }

    $user = R::load('users', $userId);

    if(isset($user) == false){
        echo 'Login error';
        exit;
    }

    echo $user['login'];
?>