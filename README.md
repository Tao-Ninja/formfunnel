# Form Submission Bot Detection and Funnel Redirection

This serverless app is Open Source and runs on C#, optimized for Azure Functions.

It utilizes Google Recaptcha v3 to detect bots and then will send non-bots along to the area you determine in your Azure Function Global Variable.

You will configure a few different Environmental Variables:

RecaptchaSecretKey = Google Recaptcha Secret Key

successTrigger = The link you want to submit form data to if the entry passes our bot detection process. 

failTrigger = The link you want to submit form data if the entry fails our bot detection process.  This is great for data analysis.

captchaScore = the bot Score Sensitivity. Set this to a value between 1 and 10. We usually choose 5 for general sensitivity. More information can be found on Google Recaptcha Score about this value.  Once in our code, we divide the value by 10 so it matches Google reCaptcha Score standards.
