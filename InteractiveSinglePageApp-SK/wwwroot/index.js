$(function () {
  $(document).scroll(function () {
    var $nav = $("#mainNavbar");
    $nav.toggleClass("scrolled", $(this).scrollTop() > $nav.height());
  });
})


let formLogin = document.querySelector('form.login');
let email = document.querySelector('#email');
let password = document.querySelector('#password');
const login = document.querySelector('#login');
const card = document.querySelector('.card');

let successFlagP = false;
let successFlagE = false;
// validation
const isValidEmail = email => {
  const re = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
  return re.test(String(email).toLowerCase());
}

formLogin.addEventListener('change', function (e) {
  validateInputs();
})

const setError = (element, message) => {
  const inputControl = element.parentElement;
  const errorDisplay = inputControl.querySelector('.error');

  errorDisplay.innerText = message;
  inputControl.classList.add('error');
  inputControl.classList.remove('success');
}

const setSuccess = (element) => {
  const inputControl = element.parentElement;
  const errorDisplay = inputControl.querySelector('.error');

  errorDisplay.innerText = '';
  inputControl.classList.add('success');
  inputControl.classList.remove('error');
}

const validateInputs = () => {
  successFlagP = false;
  successFlagE = false;
  const emailValue = email.value.trim();
  const passwordValue = password.value.trim();
  if (emailValue === '') {
    setError(email, 'Email is required.');
  } else if (!isValidEmail(emailValue)) {
    setError(email, 'Please enter a valid email.');
  } else {
    setSuccess(email);
    successFlagE = true;
  }

  if (passwordValue === '') {
    setError(password, 'Password is required.');
  } else if (passwordValue.length < 8) {
    setError(password, 'Password must be at least 8 characters.');
  } else {
    setSuccess(password);
    successFlagP = true;
  }
  if (successFlagP && successFlagE) {
    document.querySelector("#login").disabled = false;
    console.log('success')
  } else {
    document.querySelector("#login").disabled = true;
  }
  // validation end
}
validateInputs();

const validateInputsRegister = () => {
  successFlagP = false;
  successFlagE = false;
  successFlagPC = false;
  const registerButton = document.querySelector('#registerBtn');
  const emailValue = email.value.trim();
  const passwordValue = password.value.trim();
  const passwordConfValue = passwordConf.value.trim();
  if (emailValue === '') {
    setError(email, 'Email is required.');
  } else if (!isValidEmail(emailValue)) {
    setError(email, 'Please enter a valid email.');
  } else {
    setSuccess(email);
    successFlagE = true;
  }
  if (passwordValue === '') {
    setError(password, 'Password is required.');
  } else if (passwordValue.length < 8 || passwordValue.length > 20) {
    setError(password, 'Password must be between 8 & 20 characters.');
  } else {
    setSuccess(password);
    successFlagP = true;
  }
  if (passwordConfValue !== passwordValue || passwordConfValue === '') {
    setError(passwordConf, 'Please confirm your password.')
  } else {
    setSuccess(passwordConf);
    successFlagPC = true;
  }
  if (successFlagP && successFlagPC && successFlagE) {
    document.querySelector("#registerBtn").disabled = false;
  } else {
    document.querySelector("#registerBtn").disabled = true;
  }
}
document.body.addEventListener('htmx:afterRequest', function (evt) {
  console.log(evt);
  email = document.querySelector('#email');
  password = document.querySelector('#password');
  if (evt.detail.requestConfig.verb === 'get') {
    console.log("hi")
    console.log(evt.detail.requestConfig.path)
    formLogin = document.querySelector('form.login');
    if (evt.detail.requestConfig.path === '/loginPage') {
      console.log('login page loaded')
      formLogin.addEventListener('change', function (e) {
        validateInputs();
      })
      validateInputs();
      console.log('login page loaded')
    } else if (evt.detail.requestConfig.path === '/registerPage') {
      console.log('register page loaded')
      let passwordConf = document.querySelector('#passwordConf');
      let formRegister = document.querySelector('form.register');
      formRegister.addEventListener('change', function (e) {
        validateInputsRegister();
      })
      validateInputsRegister();
    } else if (evt.detail.requestConfig.path === '/logout') {
      console.log('logout post')
      htmx.ajax('GET', '/loginPage', { target: '.replace' });
      document.querySelector('.loginButton').classList.toggle('d-none');
      document.querySelector('.logoutNav').classList.toggle('d-none');
    }
  }//
  if (evt.detail.requestConfig.verb === 'post') {
    console.log("detected post")
    if (evt.detail.requestConfig.path === '/login') {
      console.log('login post')
      if (document.querySelector('.loginMessage').innerHTML === 'Login Successful') {
        htmx.ajax('GET', '/home', { target: '.replace' });
        document.querySelector('.loginButton').classList.toggle('d-none');
        document.querySelector('.logoutNav').classList.toggle('d-none');
      }
    }
  }
});