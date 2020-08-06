# Form Submission Bot Detectiona and Funnel Redirection

This serverless app is Open Source and runs on C#, optimized for Azure Functions.

It utilizes Google Recaptcha v3 to detect bots and then will send non-bots along to the area you determine in your Azure Function Global Variable.

You will configure two different Environmental Variables:

RecaptchaSecretKey = Google Recaptcha Secret Key

successTrigger = The link you want to submit form data to if the entry passes our bot detection process. 
