const login = document.querySelector('#login');
const card = document.querySelector('.card');
let successFlagP = false;
let successFlagE = false;
let successFlagPC = false;
// validation
$(function () {
  $(document).scroll(function () {
    var $nav = $("#mainNavbar");
    var scrollDistance = 1;
    $nav.toggleClass("scrolled", $(this).scrollTop() > scrollDistance);
  });
});
const isValidEmail = email => {
  const re = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
  return re.test(String(email).toLowerCase());
}
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


const validateEmail = () => {
  const emailValue = email.value.trim();
  successFlagE = false;
  if (emailValue === '') {
    setError(email, 'Email is required.');
  } else if (!isValidEmail(emailValue)) {
    setError(email, 'Please enter a valid email.');
  } else {
    setSuccess(email);
    successFlagE = true;
  }
  formLogin = document.querySelector('form.login');
  if (formLogin != null) {
    validateInputs();
  }
  formRegister = document.querySelector('form.register');
  if (formRegister != null) {
    validateInputsRegister();
  }
}
const validatePassword = () => {
  const passwordValue = password.value.trim();
  successFlagP = false;
  if (passwordValue === '') {
    setError(password, 'Password is required.');
  } else if (passwordValue.length < 8) {
    setError(password, 'Password must be at least 8 characters.');
  } else {
    setSuccess(password);
    successFlagP = true;
  }
  formLogin = document.querySelector('form.login');
  if (formLogin != null) {
    validateInputs();
  }
  formRegister = document.querySelector('form.register');
  if (formRegister != null) {
    validateInputsRegister();
  }
}
const validateConfirmPassword = () => {
  const passwordValue = password.value.trim();
  const passwordConfValue = passwordConf.value.trim();
  successFlagPC = false;
  if (passwordConfValue !== passwordValue || passwordConfValue === '') {
    setError(passwordConf, 'Please confirm your password.');
  } else {
    setSuccess(passwordConf);
    successFlagPC = true;
  }
  formRegister = document.querySelector('form.register');
  if (formRegister != null) {
    validateInputsRegister();
  }
}

const validateInputs = () => {
  if (successFlagE && successFlagP) {
    document.querySelector("#login").disabled = false;
  } else {
    document.querySelector("#login").disabled = true;
  }
}

const validateInputsRegister = () => {
  if (successFlagE && successFlagP && successFlagPC) {
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
    if (evt.detail.requestConfig.path === '/loginPage' || evt.detail.requestConfig.path === '/home') {
      console.log('login page loaded')
      if (email != null) {
        email.addEventListener('blur', function (e) {
          validateEmail();
        })
      }
      if (password != null) {
        password.addEventListener('blur', function (e) {
          validatePassword();
        })
      }
      if (formLogin != null) {
        console.log('form login')
      } else {
        document.querySelector('.loginButton').classList.add('d-none');
        document.querySelector('.logoutNav').classList.remove('d-none');
      }
      console.log('login page loaded')
    } else if (evt.detail.requestConfig.path === '/registerPage') {
      console.log('register page loaded')
      let passwordConf = document.querySelector('#passwordConf');
      let formRegister = document.querySelector('form.register');
      if (email != null) {
        email.addEventListener('blur', function (e) {
          validateEmail();
        })
      }
      if (password != null) {
        password.addEventListener('blur', function (e) {
          validatePassword();
        })
      }
      if (passwordConf != null) {
        passwordConf.addEventListener('blur', function (e) {
          validateConfirmPassword();
        })
      }
    } else if (evt.detail.requestConfig.path === '/logout') {
      console.log('logout post')
      htmx.ajax('GET', '/loginPage', { target: '.replace' });
      document.querySelector('.loginButton').classList.remove('d-none');
      document.querySelector('.logoutNav').classList.add('d-none');
    }
  }
  if (evt.detail.requestConfig.verb === 'post') {
    console.log("detected post")
    if (evt.detail.requestConfig.path === '/login') {
      console.log('login post')
      if (document.querySelector('.loginMessage').innerHTML === 'Login Successful') {
        htmx.ajax('GET', '/home', { target: '.replace' });
        document.querySelector('.loginButton').classList.add('d-none');
        document.querySelector('.logoutNav').classList.remove('d-none');
      }
    } else if (evt.detail.requestConfig.path === '/register') {
      console.log('register post')
      if (document.querySelector('.registerMsg').innerHTML === 'User Created Successfully.') {
        htmx.ajax('GET', '/loginPage', { target: '.replace' });
      }
    } else if (evt.detail.requestConfig.path === '/addFeed') {
      console.log('add feed post')
      if (document.querySelector('.addFeedMsg').innerHTML === 'Feed Added Successfully.') {
        htmx.ajax('GET', '/home', { target: '.replace' });
      }
    }
  }
  if (evt.detail.requestConfig.verb === 'delete') {
    console.log('delete feed')
    if (document.querySelector('.removeFeedMsg').innerHTML === 'Feed Removed Successfully.') {
      htmx.ajax('GET', '/home', { target: '.replace' });
    }
  }
});